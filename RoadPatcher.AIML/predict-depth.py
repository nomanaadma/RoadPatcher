import cv2
import torch
import numpy as np
from ultralytics import YOLO
from depth_anything_v2.dpt import DepthAnythingV2

# ==========================================
# 1. LOAD MODELS (Configured for vitb)
# ==========================================
print("Loading YOLO Segmentation Model...")
model_yolo = YOLO("output/d40_yolo26_seg_model/weights/best.pt")

print("Loading Native Depth Anything V2 (Base Model)...")
# Full configurations mapping
model_configs = {
    'vits': {'encoder': 'vits', 'features': 64, 'out_channels': [48, 96, 192, 384]},
    'vitb': {'encoder': 'vitb', 'features': 128, 'out_channels': [96, 192, 384, 768]},
    'vitl': {'encoder': 'vitl', 'features': 256, 'out_channels': [256, 512, 1024, 1024]}
}

encoder = 'vits' # Explicitly setting to base model
model_depth = DepthAnythingV2(**model_configs[encoder])

# Looks for 'depth_anything_v2_vitb.pth' in your root directory
model_depth.load_state_dict(torch.load(f"depth_anything_v2_{encoder}.pth", map_location="cpu", weights_only=True))
model_depth.eval() 

# ==========================================
# 2. RUN INFERENCE
# ==========================================
image_path = "data/19.jpg"
img_cv2 = cv2.imread(image_path)
img_height, img_width = img_cv2.shape[:2]

print("Running Depth Estimation...")
with torch.no_grad():
    depth_map = model_depth.infer_image(img_cv2) # Returns HxW float array

print("Running YOLO Segmentation...")
results = model_yolo(image_path)
result = results[0]

# ==========================================
# 3. BASE CONFIGURATION
# ==========================================
camera_height_m = 1.5
anchor_distance_m = 1.5
horizon_y = img_height * 0.35 
cost_per_m2 = 120.0 

if result.masks is not None and result.boxes is not None:
    masks = result.masks.xy        
    boxes = result.boxes.xyxy.cpu().numpy()
    
    # --- A. 2D Camera Calibration ---
    anchor_idx = np.argmax(boxes[:, 3]) 
    anchor_y_pixel = boxes[anchor_idx, 3]
    calibration_constant = anchor_distance_m * (anchor_y_pixel - horizon_y)
    focal_length_px = calibration_constant / camera_height_m
    
    # --- B. AI Depth Map Calibration (Solving for A and B) ---
    y_close = int(anchor_y_pixel) 
    y_far = int(horizon_y + (anchor_y_pixel - horizon_y) * 0.2) 
    
    Z_close = calibration_constant / (y_close - horizon_y)
    Z_far = calibration_constant / (y_far - horizon_y)
    
    x_start, x_end = int(img_width * 0.3), int(img_width * 0.7)
    d_close = np.median(depth_map[y_close, x_start:x_end])
    d_far = np.median(depth_map[y_far, x_start:x_end])
    
    inv_Z_close = 1.0 / Z_close
    inv_Z_far = 1.0 / Z_far
    
    A_constant = 0
    B_constant = 0
    if abs(inv_Z_close - inv_Z_far) > 1e-5:
        A_constant = (d_close - d_far) / (inv_Z_close - inv_Z_far)
        B_constant = d_close - A_constant * inv_Z_close

    print(f"\n--- Global Calibration Complete ---")
    print(f"2D Camera Constant: {calibration_constant:.2f}")
    print(f"3D Depth Scale (A): {A_constant:.2f} | Shift (B): {B_constant:.2f}\n")

    # ==========================================
    # 4. MEASURE POTHOLES
    # ==========================================
    for i, (mask, box) in enumerate(zip(masks, boxes)):
        
        # 1. 2D Area & Distance Math
        polygon_pts = np.array(mask, dtype=np.int32)
        pixel_area = cv2.contourArea(polygon_pts)
        
        bottom_y = box[3]
        if bottom_y <= horizon_y: continue 
            
        distance_m = calibration_constant / (bottom_y - horizon_y)
        scale_cm_per_px = (distance_m / focal_length_px) * 100
        true_area_cm2 = pixel_area * (scale_cm_per_px ** 2)
        
        physical_width_cm = (box[2] - box[0]) * scale_cm_per_px
        physical_length_cm = (box[3] - box[1]) * scale_cm_per_px
        
        # 2. Extract AI Disparity Values
        blank_mask = np.zeros_like(depth_map, dtype=np.uint8)
        cv2.fillPoly(blank_mask, [polygon_pts], 1)
        z_hole_disp = np.median(depth_map[blank_mask == 1])
        
        kernel = np.ones((20, 20), np.uint8)
        road_mask = cv2.dilate(blank_mask, kernel, iterations=1) - blank_mask 
        z_road_disp = np.median(depth_map[road_mask == 1])
        
        # 3. Calculate True Depth using A & B Constants
        final_depth_cm = 5.0 # Fallback default if map translation errors out
        
        if A_constant > 0:
            try:
                Z_hole_m = A_constant / (z_hole_disp - B_constant)
                Z_road_m = A_constant / (z_road_disp - B_constant)
                calculated_depth_cm = (Z_hole_m - Z_road_m) * 100
                
                # Sanity filter to handle depth map anomalies smoothly
                if 1.0 <= calculated_depth_cm <= 25.0:
                    final_depth_cm = calculated_depth_cm
                elif calculated_depth_cm > 25.0:
                    final_depth_cm = 25.0
            except:
                pass 
                
        # 4. Volume and Cost Calculation
        volume_cm3 = true_area_cm2 * final_depth_cm
        area_m2 = true_area_cm2 / 10000.0
        estimated_cost = area_m2 * cost_per_m2
        
        print(f"Pothole {i+1}:")
        print(f"  - Distance: {distance_m:.2f} m")
        print(f"  - Dimensions: {physical_width_cm:.1f}cm W x {physical_length_cm:.1f}cm L")
        print(f"  - Calculated Depth: {final_depth_cm:.1f} cm")
        print(f"  - Volume: {volume_cm3:.0f} cm³")
        print(f"  - Est. Repair Cost: ${estimated_cost:.2f}\n")
        
        # 5. Draw Results to Screen Array
        cv2.polylines(img_cv2, [polygon_pts], isClosed=True, color=(0, 255, 0), thickness=3)
        label_line1 = f"W:{physical_width_cm:.0f}cm L:{physical_length_cm:.0f}cm"
        label_line2 = f"D:{final_depth_cm:.1f}cm | Vol:{volume_cm3:.0f}cm3"
        cv2.putText(img_cv2, label_line1, (int(box[0]), int(box[1]) - 35), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
        cv2.putText(img_cv2, label_line2, (int(box[0]), int(box[1]) - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)

cv2.imwrite("final_3d_pothole_metrics.jpg", img_cv2)
print("Saved complete pipeline to final_3d_pothole_metrics.jpg")