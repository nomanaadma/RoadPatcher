from ultralytics import YOLO

# Load the cutting-edge YOLO26 segmentation model
model = YOLO("yolo26s-seg.pt") 

# Train the model on your highly specific D40 polygon dataset
results = model.train(
    data="/kaggle/working/dataset_config.yaml",
    epochs=100,             
    imgsz=640,              
    batch=32,               
    project="RoadPatcher",  
    name="d40_yolo26_seg_model",
    device=[0, 1],
    workers=4,
    
    # Critical augmentations to handle the non-fixed camera angles
    augment=True,           
    degrees=15.0,           
    perspective=0.001,      
    shear=2.0               
)s