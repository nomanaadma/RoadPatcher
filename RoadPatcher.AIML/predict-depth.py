import os
import cv2
import numpy as np
import torch
import torch.nn.functional as F
from ultralytics import YOLO

# Try importing Depth Anything V2 architecture components
try:
    from depth_anything_v2.dpt import DepthAnythingV2
except ImportError:
    raise ImportError("Please ensure the Depth-Anything-V2 repository is cloned and in your system path.")

# ==============================================================================
# PAKISTAN LOCALIZED COST CONFIGURATION VARIABLES (Modify these as market rates change)
# ==============================================================================
PKR_ASPHALT_PER_CUBIC_METER = 45000.0  # Approx cost for standard dense bituminous mix
PKR_LABOR_BASE_PER_PATCH    = 3500.0   # Fixed manual labor cost per patch deployment
PKR_MACHINERY_FUEL_OVERHEAD = 5000.0   # Roller compactors, asphalt cutters, and transport logistics

def get_birds_eye_view_ratio(img_w, img_h, camera_height, tilt_rad, fov_v_rad, fov_h_rad, target_distance):
    """
    Creates a localized perspective warper matrix at the target distance 
    to remove perspective distortion for accurate area evaluation.
    """
    # Define a 2-meter bounding patch region on the road surface around the target distance
    dist_min = max(0.5, target_distance - 1.0)
    dist_max = target_distance + 1.0
    
    # Map those real-world distances back to original vertical pixel lines
    # Formula: alpha = atan(H / D) - tilt
    angle_min = np.arctan(camera_height / dist_min) - tilt_rad
    angle_max = np.arctan(camera_height / dist_max) - tilt_rad
    
    y_pixel_min = int(((angle_min / fov_v_rad) + 0.5) * img_h)
    y_pixel_max = int(((angle_max / fov_v_rad) + 0.5) * img_h)
    
    # Clamp bounds to safely fit inside frame coordinates
    y_pixel_min = max(0, min(img_h - 1, y_pixel_min))
    y_pixel_max = max(0, min(img_h - 1, y_pixel_max))
    
    # Calculate lateral metric view width ranges across boundaries
    w_meters_near = 2 * dist_min * np.tan(fov_h_rad / 2)
    w_meters_far = 2 * dist_max * np.tan(fov_h_rad / 2)
    
    # Create target quadrilateral source coordinate references
    src_points = np.float32([
        [0, y_pixel_max],               # Top Left
        [img_w - 1, y_pixel_max],       # Top Right
        [img_w - 1, y_pixel_min],       # Bottom Right
        [0, y_pixel_min]                # Bottom Left
    ])
    
    # Project to a perfect uniform rectangular metric grid box representation
    grid_size = 500 # 500x500 pixel uniform matrix window representation
    dst_points = np.float32([
        [0, 0],
        [grid_size - 1, 0],
        [grid_size - 1, grid_size - 1],
        [0, grid_size - 1]
    ])
    
    M = cv2.getPerspectiveTransform(src_points, dst_points)
    
    # Metric conversion: How much real-world space does 1 grid pixel represent?
    avg_width_meters = (w_meters_near + w_meters_far) / 2.0
    pixel_to_meter_ratio = avg_width_meters / grid_size
    
    return M, pixel_to_meter_ratio


def load_depth_anything_v2(encoder='vitb', device='cuda'):
    """
    Instantiates Depth Anything V2 correctly using native feature allocations
    to eliminate 'vitb' KeyError issues.
    """
    model_configs = {
        'vits': {'encoder': 'vits', 'features': 64, 'out_channels': [48, 96, 192, 384]},
        'vitb': {'encoder': 'vitb', 'features': 128, 'out_channels': [96, 192, 384, 768]},
        'vitl': {'encoder': 'vitl', 'features': 256, 'out_channels': [256, 512, 1024, 1024]}
    }
    
    if encoder not in model_configs:
        raise KeyError(f"Encoder '{encoder}' not found. Choose from: {list(model_configs.keys())}")
        
    print(f"Initializing Depth Anything V2 ({encoder.upper()}) on device: {device}...")
    depth_model = DepthAnythingV2(**model_configs[encoder])
    
    # Path allocation reference
    weights_path = f"checkpoints/depth_anything_v2_{encoder}.pth"
    if os.path.exists(weights_path):
        depth_model.load_state_dict(torch.load(weights_path, map_location=device))
    else:
        print(f"Warning: Precise checkpoint weights file not found at {weights_path}. Running with uninitialized structural layers.")
        
    depth_model = depth_model.to(device).eval()
    return depth_model


def process_road_damage(image_path, model_path, output_path):
    if not os.path.exists(image_path):
        print(f"Error: Image not found at {image_path}")
        return
        
    # Read image frames
    raw_img = cv2.imread(image_path)
    img_h, img_w, _ = raw_img.shape
    
    device = 'cuda' if torch.cuda.is_available() else 'cpu'

    # Load processing modules
    print("Loading RoadPatcher Core Elements...")
    seg_model = YOLO(model_path)
    depth_model = load_depth_anything_v2(encoder='vitb', device=device)
    
    # Run inferences execution structures
    print("Executing network feature Extractions...")
    seg_results = seg_model(image_path)
    
    # Native depth prediction calculations
    with torch.no_grad():
        depth_map = depth_model.infer_image(raw_img) # Returns raw HxW relative depth map numpy float arrays

    # Camera Calibration Parameters (Modified based on your updated properties request)
    camera_height = 1.2         # Meters
    camera_tilt_deg = 30.0      # Degrees
    fov_v_deg = 90.0            # High vertical wide-angle allocation lens
    fov_h_deg = 50.0            # Horizontal view vector constraints

    tilt_rad = np.radians(camera_tilt_deg)
    fov_v_rad = np.radians(fov_v_deg)
    fov_h_rad = np.radians(fov_h_deg)

    annotated_img = seg_results[0].plot()

    if seg_results[0].masks is not None:
        print(f"Identified {len(seg_results[0].masks)} pavement defects inside inspection window.\n")
        
        for idx, mask in enumerate(seg_results[0].masks):
            polygon_pixels = mask.xy[0] 
            
            # 1. Distance tracking parameters
            lowest_y_pixel = np.max(polygon_pixels[:, 1])
            center_x = int(np.mean(polygon_pixels[:, 0]))
            center_y = int(np.mean(polygon_pixels[:, 1]))
            
            norm_y = (lowest_y_pixel / img_h) - 0.5
            pixel_angle_from_center = norm_y * fov_v_rad
            total_drop_angle = tilt_rad + pixel_angle_from_center
            
            if total_drop_angle <= 0:
                continue
                
            calculated_distance = camera_height / np.tan(total_drop_angle)
            
            # 2. Bird's Eye View Warp Area Calculations
            M_warp, metric_ratio = get_birds_eye_view_ratio(
                img_w, img_h, camera_height, tilt_rad, fov_v_rad, fov_h_rad, calculated_distance
            )
            
            # Transform contours structure array configuration elements directly onto uniform birds-eye coordinates grid layout
            poly_reshaped = polygon_pixels.reshape(-1, 1, 2).astype(np.float32)
            warped_polygon = cv2.perspectiveTransform(poly_reshaped, M_warp)
            
            # Translate metrics scaling area bounds accurately
            warped_poly_metric = warped_polygon * metric_ratio
            area_sq_meters = cv2.contourArea(warped_poly_metric.astype(np.float32))
            area_sq_cm = area_sq_meters * 10000
            
            # 3. Dynamic Depth Extractions via depth map relative values metrics tracking
            mask_binary = mask.data[0].cpu().numpy().astype(np.uint8)
            mask_binary_resized = cv2.resize(mask_binary, (img_w, img_h), interpolation=cv2.INTER_NEAREST)
            
            # Extract depth profile indexes inside pothole vs perimeter edge references boundaries
            pothole_depth_values = depth_map[mask_binary_resized == 1]
            
            if len(pothole_depth_values) > 0:
                # Find the maximum indentation dip vs entry layer threshold heights profiles
                deepest_point_relative = np.max(pothole_depth_values) 
                surface_rim_relative = np.min(pothole_depth_values)
                
                relative_delta = deepest_point_relative - surface_rim_relative
                
                # Scale relative pixel displacement depth values using geometric distance ratio properties
                # Scaler multiplier tuning parameter maps physical structural depths accurately
                depth_scaler_coefficient = 0.35 
                calculated_depth_cm = relative_delta * calculated_distance * depth_scaler_coefficient
                calculated_depth_cm = max(2.0, min(35.0, calculated_depth_cm)) # Bound output logically between 2cm and 35cm
            else:
                calculated_depth_cm = 7.5 # Fallback dynamic baseline default configuration assignment value
                
            calculated_depth_meters = calculated_depth_cm / 100.0
            
            # 4. Volumetric and Localized Costing Estimations (PKR currency structural matrices)
            volume_cubic_meters = area_sq_meters * calculated_depth_meters
            material_cost_pkr = volume_cubic_meters * PKR_ASPHALT_PER_CUBIC_METER
            
            # Total cost compounding structure
            total_cost_pkr = material_cost_pkr + PKR_LABOR_BASE_PER_PATCH + PKR_MACHINERY_FUEL_OVERHEAD
            
            # Console reporting
            print(f"--- Pothole #{idx + 1} Profile ---")
            print(f"  Distance Mapping : {calculated_distance:.2f} Meters")
            print(f"  True Surface Area: {area_sq_cm:.1f} sq cm")
            print(f"  Structural Depth : {calculated_depth_cm:.1f} cm")
            print(f"  Estimated Material Vol: {volume_cubic_meters:.5f} m3")
            print(f"  Suggested Repair Cost : Rs. {total_cost_pkr:,.0f} PKR\n")
            
            # 5. On-Screen Overlay Text Rendering Burn Pipeline
            metrics = [
                f"#{idx+1} Dist: {calculated_distance:.1f}m",
                f"Area: {area_sq_cm:.0f}cm2",
                f"Depth: {calculated_depth_cm:.1f}cm",
                f"Cost: Rs {total_cost_pkr:,.0f}"
            ]
            
            for line_idx, text in enumerate(metrics):
                y_offset = center_y - 80 + (line_idx * 60) # Maintained your 60px line spatial row tracking layout
                
                if "Cost" in text:
                    text_color = (3, 171, 95)   # Green for suggestion
                elif "Dist" in text:
                    text_color = (204, 0, 0)    # Blue/Red for spatial tracking
                elif "Depth" in text:
                    text_color = (121, 1, 100)  # Magenta for structural attributes
                else:
                    text_color = (0, 0, 0)      # Contrast fallback
                
                cv2.putText(
                    annotated_img, 
                    text, 
                    (center_x - 120, y_offset), 
                    cv2.FONT_HERSHEY_SIMPLEX, 
                    1.6, # Custom font scale parameter setup
                    text_color, 
                    5,   # Thickness profile
                    cv2.LINE_AA
                )
            
        cv2.imwrite(output_path, annotated_img)
        print(f"Inspection complete. System file generation saved to: {output_path}")
    else:
        print("No severe structural road hazards identified within the tracking grid window context.")

if __name__ == "__main__":
    INPUT_IMAGE  = "data/_1.jpeg"
    YOLO_MODEL   = "output/d40_yolo26_seg_model/weights/best.pt"
    OUTPUT_IMAGE = "19_detected.jpg"
    
    process_road_damage(INPUT_IMAGE, YOLO_MODEL, OUTPUT_IMAGE)