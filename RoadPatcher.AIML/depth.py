import torch
import cv2
import numpy as np
from depth_anything_v2.dpt import DepthAnythingV2

# 1. Setup Depth Anything V2 (Small/Vits version for speed)
model_configs = {'vits': {'encoder': 'vits', 'features': 64, 'out_channels': [48, 96, 192, 384]}}
depth_model = DepthAnythingV2(**model_configs['vits'])
depth_model.load_state_dict(torch.load('depth_anything_v2_vits.pth', map_map='cpu'))
depth_model.eval()

# 2. Run Inference on the same image
raw_img = cv2.imread("data/4.jpg")
depth_map = depth_model.infer_image(raw_img) # Returns a relative depth map

# 3. Integrate with your YOLO Loop
# (Assuming 'mask' is the polygon from your YOLO26 results)
mask_indices = np.where(yolo_mask == 1)
pothole_depth_values = depth_map[mask_indices]

# Find the 'Surface' depth (sample pixels just outside the mask)
# For simplicity, we use the median of the surrounding pixels
road_surface_relative_depth = np.median(depth_map[yolo_box_y_min-10 : yolo_box_y_min, yolo_box_x_min:yolo_box_x_max])

# Calculate Relative Difference
rel_diff = np.mean(pothole_depth_values) - road_surface_relative_depth

# 4. Convert Relative to Physical Depth (cm)
# Scaling factor: This varies by focal length, but a common approximation 
# at 3m distance is ~0.5cm per relative unit.
depth_cm = abs(rel_diff) * 0.5 * (distance_pothole / 3.0)

# 5. Final Material Calculation
volume_cm3 = true_area_cm2 * depth_cm
asphalt_kg = (volume_cm3 / 1000) * 2.32 # 2.32kg per Liter
total_cost = asphalt_kg * 0.15 # Assuming $0.15 per kg

print(f"Estimated Depth: {depth_cm:.1f} cm")
print(f"Total Asphalt Needed: {asphalt_kg:.2f} kg")
print(f"Repair Cost: ${total_cost:.2f}")