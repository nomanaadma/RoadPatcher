import cv2
import math
import numpy as np
from ultralytics import YOLO

# 1. Initialize configurations
USER_HEIGHT_M = 1.3           # e.g., 1.5 meters
USER_DISTANCE_M = 1.65         # e.g., 3 meters away
COST_PER_CUBIC_METER = 150000 # Cost in PKR
STRUCTURAL_DEPTH_M = 0.05     # 5 cm standard patching depth

def get_camera_fov(image_path):
    # Hardcoded fallback: 26mm is standard for modern smartphone wide-angle lenses
    fallback_focal_length_35mm = 26.0 
    focal_length_35mm = None
    orientation = 1  # Default EXIF orientation (1 = Normal/Landscape)
    img_width, img_height = 0, 0

    try:
        img = Image.open(image_path)
        img_width, img_height = img.size
        exif_data = img._getexif()

        if exif_data is not None:
            # Map EXIF tag IDs to human-readable names
            exif = {
                ExifTags.TAGS.get(k, k): v
                for k, v in exif_data.items()
            }

            # 1. Extract 35mm Equivalent Focal Length
            if 'FocalLengthIn35mmFilm' in exif:
                focal_length_35mm = float(exif['FocalLengthIn35mmFilm'])
                print(f"Success: Found 35mm Equivalent: {focal_length_35mm}mm")
            else:
                print("Warning: 35mm Equivalent not found. Using fallback.")

            # 2. Extract Orientation Tag
            if 'Orientation' in exif:
                orientation = exif['Orientation']
        else:
            print("Warning: No EXIF data found. Using fallbacks.")

    except Exception as e:
        print(f"Error reading image or EXIF data: {e}. Using fallbacks.")

    # Apply focal length fallback if extraction failed
    if focal_length_35mm is None:
        focal_length_35mm = fallback_focal_length_35mm
        print(f"Applied Fallback Focal Length: {focal_length_35mm}mm")

    # Standard 35mm sensor dimensions (Landscape)
    sensor_width_mm = 36.0
    sensor_height_mm = 24.0

    # Calculate base FOVs in radians, then convert to degrees
    fov_h_deg = math.degrees(2 * math.atan(sensor_width_mm / (2 * focal_length_35mm)))
    fov_v_deg = math.degrees(2 * math.atan(sensor_height_mm / (2 * focal_length_35mm)))

    # --- Orientation Logic ---
    # EXIF Orientation codes: 
    # 1: Normal (Landscape)
    # 6: Rotated 90 CW (Portrait)
    # 8: Rotated 90 CCW (Portrait)
    # 5, 7: Mirrored Portraits (Rare, mostly front-facing cameras)
    is_portrait_exif = orientation in [5, 6, 7, 8]
    
    # Fallback: If EXIF says landscape (or is missing) but the image pixels are taller than wide
    is_portrait_dimensions = img_height > img_width

    # If either the EXIF data or the physical pixels indicate portrait mode, swap the FOV
    if is_portrait_exif or is_portrait_dimensions:
        print("Orientation: Portrait Detected. Swapping Horizontal and Vertical FOVs.")
        # Swap the variables
        fov_h_deg, fov_v_deg = fov_v_deg, fov_h_deg
    else:
        print("Orientation: Landscape Detected.")

    return fov_h_deg, fov_v_deg



def process_road_damage(image_path):
    # Step 1: Extract FOV using the function we built earlier
    fov_h_deg, fov_v_deg = get_camera_fov(image_path)
    
    # Step 2: Calculate Camera Tilt Angle (in radians)
    tilt_rad = math.atan(USER_HEIGHT_M / USER_DISTANCE_M)
    
    # Step 3: Run YOLO Inference
    model = YOLO("output/d40_yolo26_seg_model/weights/best.pt")
    results = model(image_path)
    img = cv2.imread(image_path)
    img_h, img_w, _ = img.shape
    
    if results[0].masks is not None:
        for mask in results[0].masks:
            # Step 4: Get mask pixel coordinates
            polygon_pixels = mask.xy[0] 
            
            # Find extreme points (Top, Bottom, Left, Right)
            min_y_pixel = np.min(polygon_pixels[:, 1])
            max_y_pixel = np.max(polygon_pixels[:, 1])
            min_x_pixel = np.min(polygon_pixels[:, 0])
            max_x_pixel = np.max(polygon_pixels[:, 0])




            # Step 5: Apply Pinhole Math to extremes


            # 1. Convert FOVs to radians for trigonometric functions
            fov_v_rad = math.radians(fov_v_deg)
            fov_h_rad = math.radians(fov_h_deg)

            # 2. Calculate camera focal length in pixels
            # This bridges the gap between digital pixels and physical angles
            focal_length_y_px = (img_h / 2) / math.tan(fov_v_rad / 2)
            focal_length_x_px = (img_w / 2) / math.tan(fov_h_rad / 2)


            # Helper function: Calculate ground distance (Z) for any given Y-axis pixel
            def get_ground_distance(y_pixel):
                # How far off the vertical center is this pixel? 
                # (Standard OpenCV: 0 is top of image, img_h is bottom)
                dy = y_pixel - (img_h / 2)
                
                # Calculate the angular offset of this specific pixel
                alpha = math.atan(dy / focal_length_y_px)
                
                # Add camera's physical downward tilt to find the true ray angle
                gamma = tilt_rad + alpha
                
                # Prevent math errors if a pixel is strictly on or above the horizon
                if gamma <= 0: 
                    return float('inf') 
                    
                # Calculate exact distance along the ground
                return USER_HEIGHT_M / math.tan(gamma)


            # --- Calculate True Length (Y-Axis) ---
            # min_y_pixel is visually the "top" of the pothole (furthest away from you)
            # max_y_pixel is visually the "bottom" of the pothole (closest to you)
            distance_far_m = get_ground_distance(min_y_pixel)
            distance_near_m = get_ground_distance(max_y_pixel)
            
            calculated_length_m = distance_far_m - distance_near_m

            # --- Calculate True Width (X-Axis) ---
            # Find the ground distance to the exact center of the pothole to establish our scale
            center_y = (min_y_pixel + max_y_pixel) / 2
            distance_center_m = get_ground_distance(center_y)
            
            # Count how many pixels wide the hole is
            pixel_width = max_x_pixel - min_x_pixel
            
            # Scale the pixel width into real-world meters based on its distance from the camera
            calculated_width_m = distance_center_m * (pixel_width / focal_length_x_px)


            # Provide fail-safes in case of horizon anomalies
            if calculated_length_m < 0 or calculated_length_m == float('inf'):
                calculated_length_m = 0
                print("Warning: Pothole extends beyond calculated horizon.")


            
            # Step 6: Calculate Area, Volume, and Financials
            area_sq_m = calculated_length_m * calculated_width_m
            volume_m3 = area_sq_m * STRUCTURAL_DEPTH_M
            total_cost = volume_m3 * COST_PER_CUBIC_METER

            print(area_sq_m * 10000)


            
            # Step 7: Burn Metrics onto the Image Canvas
            center_x = int(np.mean(polygon_pixels[:, 0]))
            center_y = int(np.mean(polygon_pixels[:, 1]))
            
            metrics_text = f"Cost: Rs. {total_cost:.0f} PKR"
            cv2.putText(img, metrics_text, (center_x - 50, center_y), 
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 3)

    # Save or Display final output
    cv2.imwrite("output_processed.jpg", img)
    print("Processing Complete.")

process_road_damage("data/_3.jpeg");


