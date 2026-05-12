from ultralytics import YOLO

model = YOLO("output/d40_yolo26_seg_model/weights/best.pt")

results = model("data/32.jpg")

results[0].show()

