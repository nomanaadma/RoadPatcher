import os
import cv2
import math
import numpy as np
import torch
from PIL import Image, ExifTags
from ultralytics import YOLO
from depth_anything_v2.dpt import DepthAnythingV2

# 1. Initialize configurations
USER_HEIGHT_M = 1.3           # e.g., 1.5 meters
USER_DISTANCE_M = 2.0         # e.g., 3 meters away
COST_PER_CUBIC_METER = 150000 # Cost in PKR
STRUCTURAL_DEPTH_M = 0.05     # 5 cm standard patching depth


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
    weights_path = f"depth_anything_v2_{encoder}.pth"
    if os.path.exists(weights_path):
        depth_model.load_state_dict(torch.load(weights_path, map_location=device))
    else:
        print(f"Warning: Precise checkpoint weights file not found at {weights_path}. Running with uninitialized structural layers.")
        
    depth_model = depth_model.to(device).eval()
    return depth_model


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
    is_portrait_exif = orientation in [5, 6, 7, 8]
    is_portrait_dimensions = img_height > img_width

    # If either the EXIF data or the physical pixels indicate portrait mode, swap the FOV
    if is_portrait_exif or is_portrait_dimensions:
        print("Orientation: Portrait Detected. Swapping Horizontal and Vertical FOVs.")
        fov_h_deg, fov_v_deg = fov_v_deg, fov_h_deg
    else:
        print("Orientation: Landscape Detected.")

    return fov_h_deg, fov_v_deg

# --- NEW CROPPING LOGIC ADDED HERE ---
def get_cropped_depth_map(img, polygon_pixels, padding=50):
    img_h, img_w = img.shape[:2]

    # 1. Find the strict bounding box of the YOLO mask
    min_x = int(np.min(polygon_pixels[:, 0]))
    max_x = int(np.max(polygon_pixels[:, 0]))
    min_y = int(np.min(polygon_pixels[:, 1]))
    max_y = int(np.max(polygon_pixels[:, 1]))

    # 2. Add padding to capture the healthy road baseline
    crop_x1 = max(0, min_x - padding)
    crop_y1 = max(0, min_y - padding)
    crop_x2 = min(img_w, max_x + padding)
    crop_y2 = min(img_h, max_y + padding)

    # 3. Crop the original image array
    cropped_img = img[crop_y1:crop_y2, crop_x1:crop_x2]

    # 4. Shift the YOLO polygon coordinates to match the new cropped space
    shifted_polygon = polygon_pixels.copy()
    shifted_polygon[:, 0] -= crop_x1
    shifted_polygon[:, 1] -= crop_y1

    return cropped_img, shifted_polygon


def calculate_dynamic_depth(mask_polygon, depth_map, absolute_distance_m):
    # Create a mask for the pothole interior
    interior_mask = np.zeros(depth_map.shape, dtype=np.uint8)
    cv2.fillPoly(interior_mask, [mask_polygon.astype(np.int32)], 255)
    
    # Create a dilated mask to sample the healthy road surface right outside the hole
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (15, 15))
    dilated_mask = cv2.dilate(interior_mask, kernel, iterations=1)
    exterior_mask = cv2.subtract(dilated_mask, interior_mask)
    
    # Extract depth map intensities
    surface_pixels = depth_map[exterior_mask == 255]
    bottom_pixels = depth_map[interior_mask == 255]
    
    if len(surface_pixels) == 0 or len(bottom_pixels) == 0:
        return 0.05 # Safe fallback if sampling fails
        
    # Average relative values
    avg_surface_relative = np.mean(surface_pixels)
    min_bottom_relative = np.min(bottom_pixels) # Deepest point
    
    # Calculate relative difference
    relative_delta = avg_surface_relative - min_bottom_relative
    
    # Scale relative difference to absolute meters
    calibration_constant = 0.01
    absolute_depth_m = relative_delta * calibration_constant * (absolute_distance_m ** 2)

    # Printing for debug visualization
    print(f"DEBUG: Absolute Depth M: {absolute_depth_m:.5f} | Avg Surface: {avg_surface_relative:.2f} | Min Bottom: {min_bottom_relative:.2f}")

    return absolute_depth_m



def process_road_damage(image_path):
    # Step 1: Extract FOV 
    fov_h_deg, fov_v_deg = get_camera_fov(image_path)
    
    # Step 2: Calculate Camera Tilt Angle (in radians)
    tilt_rad = math.atan(USER_HEIGHT_M / USER_DISTANCE_M)
    
    # Step 3: Initialize Models ONCE before the loop
    model = YOLO("output/d40_yolo26_seg_model/weights/best.pt")
    depth_model = load_depth_anything_v2(encoder='vits', device='cpu') # Moved outside the loop
    
    results = model(image_path)
    img = cv2.imread(image_path)
    img_h, img_w, _ = img.shape
    
    if results[0].masks is not None:
        for mask in results[0].masks:
            # Step 4: Get mask pixel coordinates
            polygon_pixels = mask.xy[0] 
            
            min_y_pixel = np.min(polygon_pixels[:, 1])
            max_y_pixel = np.max(polygon_pixels[:, 1])
            min_x_pixel = np.min(polygon_pixels[:, 0])
            max_x_pixel = np.max(polygon_pixels[:, 0])

            # Step 5: Apply Pinhole Math to extremes
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
                print("Warning: Pothole extends beyond calculated horizon.")

            # --- NEW: Crop and Infer Depth ---
            # 1. Crop the image to force the depth model to normalize the contrast
            cropped_img, shifted_polygon = get_cropped_depth_map(img, polygon_pixels, padding=50)

            # 2. Pass ONLY the cropped image into Depth Anything
            relative_depth_map = depth_model.infer_image(cropped_img)

            # 3. Calculate dynamic depth using the SHIFTED polygon coordinates
            DYNAMIC_DEPTH_M = calculate_dynamic_depth(shifted_polygon, relative_depth_map, distance_center_m)

            # Step 6: Calculate Area, Volume, and Financials
            area_sq_m = calculated_length_m * calculated_width_m
            volume_m3 = area_sq_m * DYNAMIC_DEPTH_M
            total_cost = volume_m3 * COST_PER_CUBIC_METER

            print(f"Area (sq cm): {area_sq_m * 10000:.2f}, Depth (m): {DYNAMIC_DEPTH_M:.5f}")
            
            # Step 7: Burn Metrics onto the Image Canvas
            center_x = int(np.mean(polygon_pixels[:, 0]))
            center_y = int(np.mean(polygon_pixels[:, 1]))
            
            metrics_text = f"Cost: Rs. {total_cost:.0f} PKR"
            cv2.putText(img, metrics_text, (center_x - 50, center_y), 
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 3)

    cv2.imwrite("output_processed.jpg", img)
    print("Processing Complete.")

process_road_damage("data/_3.jpeg")