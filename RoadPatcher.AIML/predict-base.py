import cv2
import numpy as np
from ultralytics import YOLO




model = YOLO("output/d40_yolo26_seg_model/weights/best.pt")
image_path = "local-road-images/4.jpeg"
results = model(image_path)

camera_height = 1.5
distance_pothole = 3

for r in results:
    r.names = {0: '0', 1: ''}

results[0].show()

