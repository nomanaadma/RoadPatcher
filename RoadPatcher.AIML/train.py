from ultralytics import YOLO

# Load the YOLO26 nano model (best for speed/edge deployment)
model = YOLO("yolo26s.pt") 

# Train the model on your dataset
results = model.train(
    data    = "data.yaml",
    epochs  = 25,
    imgsz   = 640,
    batch   = 16,
    name    = "rdd2022_yolo26s",
    patience= 10,
    device  = 0,
    cos_lr  = True,
)