import os
import cv2
import numpy as np
from ultralytics import YOLO

def process_road_damage(image_path, model_path, output_path):
    # 1. Verify and Load Image
    if not os.path.exists(image_path):
        print(f"Error: Image not found at {image_path}")
        return
        
    img = cv2.imread(image_path)
    img_h, img_w, _ = img.shape

    # 2. Load YOLO Segmentation Model
    print("Loading YOLO Segmentation Model...")
    model = YOLO(model_path)
    results = model(image_path)

    # 3. Camera Geometry & Calibration Parameters
    camera_height = 1.2         # Vertical height from ground to lens (Meters)
    camera_tilt_deg = 30.0      # Downward pitch angle of phone relative to horizon (Degrees)
    fov_v_deg = 90.0            # Vertical Field of View of your camera lens (Degrees)
    fov_h_deg = 50.0            # Horizontal Field of View of your camera lens (Degrees)

    # Convert degrees to radians for mathematical calculations
    tilt_rad = np.radians(camera_tilt_deg)
    fov_v_rad = np.radians(fov_v_deg)
    fov_h_rad = np.radians(fov_h_deg)

    # 4. Generate Base Background for Annotations
    annotated_img = results[0].plot()

    print(f"\nProcessing frame: {image_path} ({img_w}x{img_h})")
    
    # Check if any segmentation masks were detected
    if results[0].masks is not None:
        print(f"Detected {len(results[0].masks)} potential road defects.\n")
        
        # Loop through each mask independently
        for idx, mask in enumerate(results[0].masks):
            # Extract precise contour pixel arrays [X, Y]
            polygon_pixels = mask.xy[0] 
            
            # Find the lowest point of the pothole structure in the image plane (Max Y coordinate)
            lowest_y_pixel = np.max(polygon_pixels[:, 1])
            
            # Find the center pixel of the pothole to position our onscreen text overlay cleanly
            center_x = int(np.mean(polygon_pixels[:, 0]))
            center_y = int(np.mean(polygon_pixels[:, 1]))
            
            # --- Dynamic Distance Logic ---
            norm_y = (lowest_y_pixel / img_h) - 0.5
            pixel_angle_from_center = norm_y * fov_v_rad
            total_drop_angle = tilt_rad + pixel_angle_from_center
            
            if total_drop_angle <= 0:
                print(f"Pothole #{idx+1}: Outside physical ground grid boundary. Skipping calculation.")
                continue
                
            calculated_distance = camera_height / np.tan(total_drop_angle)
            
            # --- Real-World Area Logic ---
            scene_width_meters = 2 * calculated_distance * np.tan(fov_h_rad / 2)
            pixel_to_meter_ratio = scene_width_meters / img_w
            
            polygon_meters = polygon_pixels * pixel_to_meter_ratio
            area_sq_meters = cv2.contourArea(polygon_meters.astype(np.float32))
            area_sq_cm = area_sq_meters * 10000  # Convert to square centimeters for readability
            
            # --- Structural Estimations & Financial Engine ---
            # Fallback average depth constant (approx 7.5 cm thickness)
            # If you hook up Depth Anything V2 later, change this static value to your dynamic reading
            estimated_depth_meters = 0.075 
            estimated_depth_cm = estimated_depth_meters * 100
            
            volume_cubic_meters = area_sq_meters * estimated_depth_meters
            
            COST_PER_CUBIC_METER = 150.0  
            base_material_fee = volume_cubic_meters * COST_PER_CUBIC_METER
            logistics_mobilization_fee = 45.0
            
            total_suggested_cost = base_material_fee + logistics_mobilization_fee
            
            # 5. Log Analysis Data to Terminal Console
            print(f"--- Pothole #{idx + 1} ---")
            print(f"  Calculated Distance : {calculated_distance:.2f} meters away")
            print(f"  Estimated Area      : {area_sq_cm:.1f} sq cm")
            print(f"  Estimated Depth     : {estimated_depth_cm:.1f} cm")
            print(f"  Suggested Patch Cost: ${total_suggested_cost:.2f}\n")
            
            # 6. Burn Metrics Data Explicitly onto the Image Canvas
            # We construct a 4-line data stack to render right over the pothole center
            metrics = [
                f"#{idx+1} Dist: {calculated_distance:.1f}m",
                f"Area: {area_sq_cm:.0f}cm2",
                f"Depth: {estimated_depth_cm:.1f}cm",
                f"Cost: ${total_suggested_cost:.2f}"
            ]
            
            # Loop through the list to draw each metric on a separate vertical line
            for line_idx, text in enumerate(metrics):
                y_offset = center_y - 20 + (line_idx * 60) # 18 pixels spacing between text rows
                
                # Determine colors to separate data blocks visually
                if "Cost" in text:
                    text_color = (3, 95, 171)   # Green for financial suggestion
                elif "Dist" in text:
                    text_color = (0, 0, 204)   # Red for spatial tracking
                elif "Depth" in text:
                    text_color = (121, 1, 100)   # Red for spatial tracking
                else:
                    text_color = (0, 0, 0) # Cyan/Yellow for structural attributes
                
                cv2.putText(
                    annotated_img, 
                    text, 
                    (center_x - 50, y_offset), 
                    cv2.FONT_HERSHEY_SIMPLEX, 
                    1.6,             # Slightly scaled down font size to fit neatly
                    text_color, 
                    5
                )
            
        # 7. Write Complete Final Output Image File back to System Storage
        success = cv2.imwrite(output_path, annotated_img)
        if success:
            print(f"Processing successful! Output saved at: {output_path}")
        else:
            print("Processing Error: Unable to write to target disk path.")
    else:
        print("No structural road damage detected in target image frame.")

# Execute Main Run Execution Structure
if __name__ == "__main__":
    INPUT_IMAGE = "data/19.jpg"
    YOLO_MODEL  = "output/d40_yolo26_seg_model/weights/best.pt"
    OUTPUT_IMAGE = "19_detected.jpg"
    
    process_road_damage(INPUT_IMAGE, YOLO_MODEL, OUTPUT_IMAGE)