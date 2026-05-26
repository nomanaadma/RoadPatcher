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
# Define standard civil engineering depths for different damage types
SEVERITY_TIERS = {
    "minor": 0.02,        # 2 cm: Surface-level damage
    "standard": 0.05,     # 5 cm: Typical pothole
    "moderate": 0.08,     # 8 cm: Moderate structural damage
    "severe": 0.10,       # 10 cm: Deep pothole / exposed base
    "critical": 0.12      # 12 cm: Major structural failure
}

# Simulate the user selecting the damage type in your mobile app
# For your uploaded image (_2.jpeg), "delamination" is the correct classification
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
    img = cv2.imread(image_path)
    img_h, img_w, _ = img.shape
    
    if results[0].masks is not None:
        for mask in results[0].masks:
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

            # --- Method 3 Implementation ---
            # Fetch the depth directly from the dictionary based on the assigned severity
            DYNAMIC_DEPTH_M = SEVERITY_TIERS[CURRENT_DAMAGE_SEVERITY]

            # Calculate Area, Volume, and Financials
            area_sq_m = calculated_length_m * calculated_width_m
            volume_m3 = area_sq_m * DYNAMIC_DEPTH_M
            total_cost = volume_m3 * COST_PER_CUBIC_METER

            print(f"Severity: {CURRENT_DAMAGE_SEVERITY.upper()} | Depth applied: {DYNAMIC_DEPTH_M}m")
            print(f"Calculated Area: {area_sq_m * 10000:.1f} sq cm")
            print(f"Suggested Cost: Rs. {total_cost:.0f} PKR")
            
            # Burn Metrics onto the Image Canvas
            center_x = int(np.mean(polygon_pixels[:, 0]))
            center_y = int(np.mean(polygon_pixels[:, 1]))
            
            metrics_text = f"Cost: Rs. {total_cost:.0f} PKR"
            cv2.putText(img, metrics_text, (center_x - 50, center_y), 
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 3)

    cv2.imwrite("output_processed.jpg", img)
    print("Processing Complete.")

process_road_damage("data/4.jpg")