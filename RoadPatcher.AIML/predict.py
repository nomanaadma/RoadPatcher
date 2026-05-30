import math
import cv2
import numpy as np
from PIL import Image, ExifTags
from ultralytics import YOLO

# 1. Initialize configurations
USER_HEIGHT_M = 1.3            
USER_DISTANCE_M = 1.7          
COST_PER_CUBIC_METER = 150000  

# --- Method 3: Severity Depth Mapping ---
SEVERITY_TIERS = {
    "minor": 0.0254,      # 1 inch
    "standard": 0.0508,   # 2 inches
    "moderate": 0.0762,   # 3 inches
    "severe": 0.1016,     # 4 inches
    "critical": 0.1270    # 5 inches
}

# Simulate the user selecting the damage type in your mobile app
CURRENT_DAMAGE_SEVERITY = "standard" 

def get_camera_fov(image_path):
    fallback_focal_length_35mm = 26.0 
    focal_length_35mm = None
    orientation = 1  
    img_width, img_height = 0, 0

    try:
        img = Image.open(image_path)
        img_width, img_height = img.size
        exif_data = img._getexif()

        if exif_data is not None:
            exif = {ExifTags.TAGS.get(k, k): v for k, v in exif_data.items()}
            if 'FocalLengthIn35mmFilm' in exif:
                focal_length_35mm = float(exif['FocalLengthIn35mmFilm'])
            if 'Orientation' in exif:
                orientation = exif['Orientation']
    except Exception:
        pass

    if focal_length_35mm is None:
        focal_length_35mm = fallback_focal_length_35mm

    sensor_width_mm = 36.0
    sensor_height_mm = 24.0

    fov_h_deg = math.degrees(2 * math.atan(sensor_width_mm / (2 * focal_length_35mm)))
    fov_v_deg = math.degrees(2 * math.atan(sensor_height_mm / (2 * focal_length_35mm)))

    if orientation in [5, 6, 7, 8] or img_height > img_width:
        fov_h_deg, fov_v_deg = fov_v_deg, fov_h_deg

    return fov_h_deg, fov_v_deg

def process_road_damage(image_path):
    fov_h_deg, fov_v_deg = get_camera_fov(image_path)
    tilt_rad = math.atan(USER_HEIGHT_M / USER_DISTANCE_M)
    
    # Initialize YOLO ONCE
    model = YOLO("output/d40_yolo26_seg_model/weights/best.pt")
    
    results = model(image_path)

    for r in results:
        r.names = {0: '0', 1: ''}
    
    # --- CHANGE 1: Use YOLO's plot function to get the masked image array ---
    annotated_img = results[0].plot()
    img_h, img_w, _ = annotated_img.shape

   
    
    if results[0].masks is not None:
        # --- CHANGE 2: Use enumerate to track the pothole index (idx) ---
        # for idx, (mask) in enumerate(results[0].masks):
        for idx, (mask, box) in enumerate(zip(results[0].masks, results[0].boxes)):
            polygon_pixels = mask.xy[0] 
            
            min_y_pixel = np.min(polygon_pixels[:, 1])
            max_y_pixel = np.max(polygon_pixels[:, 1])
            min_x_pixel = np.min(polygon_pixels[:, 0])
            max_x_pixel = np.max(polygon_pixels[:, 0])

            fov_v_rad = math.radians(fov_v_deg)
            fov_h_rad = math.radians(fov_h_deg)

            focal_length_y_px = (img_h / 2) / math.tan(fov_v_rad / 2)
            focal_length_x_px = (img_w / 2) / math.tan(fov_h_rad / 2)

            def get_ground_distance(y_pixel):
                dy = y_pixel - (img_h / 2)
                alpha = math.atan(dy / focal_length_y_px)
                gamma = tilt_rad + alpha
                
                if gamma <= 0: 
                    return float('inf') 
                return USER_HEIGHT_M / math.tan(gamma)

            distance_far_m = get_ground_distance(min_y_pixel)
            distance_near_m = get_ground_distance(max_y_pixel)
            calculated_length_m = distance_far_m - distance_near_m

            center_y = (min_y_pixel + max_y_pixel) / 2
            distance_center_m = get_ground_distance(center_y)
            
            pixel_width = max_x_pixel - min_x_pixel
            calculated_width_m = distance_center_m * (pixel_width / focal_length_x_px)

            if calculated_length_m < 0 or calculated_length_m == float('inf'):
                calculated_length_m = 0

            # Fetch the depth directly from the dictionary
            DYNAMIC_DEPTH_M = SEVERITY_TIERS[CURRENT_DAMAGE_SEVERITY]

            # Calculate Area, Volume, and Financials
            area_sq_m = calculated_length_m * calculated_width_m
            volume_m3 = area_sq_m * DYNAMIC_DEPTH_M
            total_cost = volume_m3 * COST_PER_CUBIC_METER

            print(f"--- Pothole #{idx + 1} ---")
            print(f"Severity: {CURRENT_DAMAGE_SEVERITY.upper()} | Depth applied: {DYNAMIC_DEPTH_M}m")
            print(f"Calculated Area: {area_sq_m * 10000:.1f} sq cm")
            print(f"Suggested Cost: Rs. {total_cost:.0f} PKR\n")
            
            # --- BURN METRICS ONTO THE ANNOTATED IMAGE ---
            center_x = int(np.mean(polygon_pixels[:, 0]))
            center_y = int(np.mean(polygon_pixels[:, 1]))
            
            pothole_num_text = f"#{idx + 1}"
            cost_text = f"Rs. {total_cost:.0f}"


           # --- BURN METRICS ONTO THE ANNOTATED IMAGE ---
            
            # 1. Extract the bounding box's exact top-left corner
            x_min, y_min, x_max, y_max = box.xyxy[0]
            
            # 2. Format the text you want to display
            display_text = f"#{idx + 1}"

            # 3. Calculate position: Shift Right (+70px) to clear the "0.55" text 
            #    and shift slightly up (-5px) to align it horizontally with YOLO's label.
            text_x = int(x_min) + 90
            text_y = int(y_min) - 5
            
            # Failsafe: If the pothole is at the absolute top edge of the image,
            # YOLO pushes its own label inside the box. We must do the same.
            if text_y < 15:
                text_y = int(y_min) + 25

            # Draw the text in black with a clean, readable font size
            cv2.putText(annotated_img, display_text, (text_x, text_y), 
                        cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 0), 2)


            # Draw the Pothole Number in yellow, slightly offset upwards
            # cv2.putText(annotated_img, pothole_num_text, (center_x - 30, center_y), 
                        # cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 0), 3)

    # Save the final marked-up image
    cv2.imwrite("output_processed.jpg", annotated_img)
    print("Processing Complete. Saved to output_processed.jpg")

process_road_damage("data/4.jpg")