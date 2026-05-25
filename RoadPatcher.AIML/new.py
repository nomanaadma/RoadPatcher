import math
from PIL import Image, ExifTags

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

# --- Example Usage ---
image_path = "data/_3.jpeg" 
horizontal_fov, vertical_fov = get_camera_fov(image_path)

print(f"\nFinal Horizontal FOV: {horizontal_fov:.2f}°")
print(f"Final Vertical FOV: {vertical_fov:.2f}°")