import cv2
import numpy as np
from ultralytics import YOLO



# 1. Load Model & Run Inference
model = YOLO("output/d40_yolo26_seg_model/weights/best.pt")
image_path = "data/19.jpg"
results = model(image_path)  # Run inference with a confidence threshold of 25%
# results = model(image_path, conf=0.50)  # Run inference with a confidence threshold of 25%


result = results[0]  # Grab the first image result

# Read the image with OpenCV to get dimensions for math
img = cv2.imread(image_path)
img_height, img_width = img.shape[:2]

# 2. User Inputs (The "Anchor" Data)
camera_height_m = 1.5
anchor_distance_m = 1.5

# 3. Geometric Assumptions
# The horizon line is where the road meets the sky. 
# For a standard photo taken pointing slightly down at the street, it sits around the top 35%.
horizon_y = img_height * 0.35 

# --- THE GEOMETRY ENGINE ---

if result.masks is not None and result.boxes is not None:
    # Extract the polygons and bounding boxes
    masks = result.masks.xy        
    boxes = result.boxes.xyxy.cpu().numpy() # [x_min, y_min, x_max, y_max]
    
    # Step A: Find the Anchor Pothole
    # We find the box whose bottom edge (y_max) is lowest on the screen (highest pixel value)
    anchor_idx = np.argmax(boxes[:, 3]) 
    anchor_y_pixel = boxes[anchor_idx, 3]
    
    # Step B: Calculate the Camera Calibration Constant
    # Because Distance = Constant / (Y_pixel - Horizon_Y)
    # We invert it: Constant = Distance * (Y_pixel - Horizon_Y)
    calibration_constant = anchor_distance_m * (anchor_y_pixel - horizon_y)
    
    # Step C: Calculate Virtual Focal Length
    # Required to determine the cm/pixel scale at different depths
    focal_length_px = calibration_constant / camera_height_m
    
    print(f"--- Calibration Complete ---")
    print(f"Anchor found at Y: {anchor_y_pixel:.0f}px | Constant: {calibration_constant:.2f}\n")

    # Step D: Loop through all detected potholes to calculate True Area
    for i, (mask, box) in enumerate(zip(masks, boxes)):
        
        # 1. Calculate RAW Pixel Area of the irregular polygon
        polygon_pts = np.array(mask, dtype=np.int32)
        pixel_area = cv2.contourArea(polygon_pts)
        
        # 2. Calculate the specific Distance to THIS pothole
        bottom_y = box[3]
        if bottom_y <= horizon_y:
            continue # Ignore false positives positioned in the sky/above horizon
            
        distance_m = calibration_constant / (bottom_y - horizon_y)
        
        # 3. Calculate Scale Factor at this specific distance (Meters per Pixel)
        # Objects further away have a higher cm/px ratio.
        scale_m_per_px = distance_m / focal_length_px
        scale_cm_per_px = scale_m_per_px * 100
        
        # 4. Calculate True Physical Area
        # Area scale is squared because it's 2D (cm * cm)
        true_area_cm2 = pixel_area * (scale_cm_per_px ** 2)
        
        print(f"Pothole {i+1}:")
        print(f"  - Distance from Camera: {distance_m:.2f} meters")
        print(f"  - Scale at depth: {scale_cm_per_px:.2f} cm/px")
        print(f"  - True Surface Area: {true_area_cm2:.2f} cm²\n")
        
        # Optional: Draw the data directly onto the image
        cv2.polylines(img, [polygon_pts], isClosed=True, color=(0, 0, 204), thickness=5)
        cv2.putText(img, f"{distance_m:.1f}m | {true_area_cm2:.0f}cm2", 
                    (int(box[0]), int(box[1])-10),  
                    cv2.FONT_HERSHEY_SIMPLEX, 1.6, (0, 0, 204), 5)



# 1. Create a named window first
# cv2.namedWindow("RoadPatcher Metric Output", cv2.WINDOW_NORMAL)

# 2. Force the window to respect the image's aspect ratio
# cv2.setWindowProperty("RoadPatcher Metric Output", cv2.WND_PROP_ASPECT_RATIO, cv2.WINDOW_KEEPRATIO)


# scale_percent = 0.3
# width = int(img.shape[1] * scale_percent)
# height = int(img.shape[0] * scale_percent)
# dim = (width, height)



# Resize for previewing only
# preview_img = cv2.resize(img, dim, interpolation=cv2.INTER_AREA)



# --- Maintain aspect ratio ---
h, w = img.shape[:2]

max_width = 1280
max_height = 720

scale = min(max_width / w, max_height / h)

new_width = int(w * scale)
new_height = int(h * scale)

# cv2.namedWindow("RoadPatcher Metric Output", cv2.WINDOW_NORMAL)
# cv2.resizeWindow("RoadPatcher Metric Output", new_width, new_height)

# cv2.imshow("RoadPatcher Metric Output", img)

cv2.imwrite("road_pothole_metric.jpg", img)

# Display the final output
# cv2.imshow("RoadPatcher Metric Output", preview_img)
# cv2.waitKey(0)
# cv2.destroyAllWindows()