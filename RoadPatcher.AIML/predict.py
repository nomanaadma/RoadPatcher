from ultralytics import YOLO

# model = YOLO("output-/rdd2022_yolo26s/weights/best-o8.pt")/
model = YOLO("output/rdd2022_yolo26m/weights/best.pt")




# results = model(
#     "road.jpg",
#     conf=0.15,
#     classes=[3]   # only pothole
# )



results = model("road.jpg")

results[0].show()



# import cv2
# from ultralytics import YOLO

# # Load your custom trained YOLO26 medium model
# model = YOLO("output/rdd2022_yolo26s/weights/best-o11.pt")


# # Define the source (can be a local image, a video file, or a webcam stream like 0)
# source = "road.jpg"

# # Run inference
# results = model(source) 

# # Process and visualize the results
# for result in results:
#     # Display the annotated image with bounding boxes
#     result.show()
    
#     # Save the annotated image to disk
#     result.save(filename="road_crack_detected.jpg")
    
#     # Access bounding box data and specific classes programmatically
#     boxes = result.boxes
#     for box in boxes:
#         class_id = int(box.cls[0])
#         confidence = float(box.conf[0])
#         print(f"Detected RDD2022 class {class_id} with confidence {confidence:.2f}")