import os
import time
import sys
import json

import cv2
import numpy as np
from PIL import Image
from tqdm import tqdm

from yolo import YOLO

# 待适配

def predict(mode,path,savepath,fps=0):
    yolo = YOLO()
    # mode = "video"
    # predict
    crop = True
    count = True
    # video
    video_path = path#"videos/01.mp4"
    #video_path = 0  # Use camera, press ESC to exit.
    video_save_path = savepath#"videos_out/01.mp4"
    video_fps = fps#60.0
    # fps
    test_interval = 100
    fps_image_path = "imgs/01.jpg"
    # dir_predict
    dir_origin_path = path#"tmp/ori"
    dir_save_path = savepath#"tmp/det"
    # heatmap
    heatmap_save_path = "imgs_out/heatmap.png"
    # export_onnx
    simplify = True
    onnx_save_path = "../../model_data/models.onnx"
    if mode == "predict":
        #while True:
        img = path#input('Input image filename:')
        try:
            image = Image.open(img)
        except:
            raise ValueError('Open error!')
        else:
            r_image, _, perception = yolo.detect_image(image, img, crop=crop, count=count)
            r_image.save(savepath)
            print(json.dumps(perception))
    elif mode == "video":
        capture = cv2.VideoCapture(video_path)
        #if video_save_path != "":
        #    video_save_path = "videos_out/01.avi"
        fourcc = cv2.VideoWriter_fourcc(*'AVC1')
        size = (int(capture.get(cv2.CAP_PROP_FRAME_WIDTH)), int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT)))
        out = cv2.VideoWriter(video_save_path, fourcc, video_fps, size)
        ref, frame = capture.read()
        if not ref:
            raise ValueError("Failed to read the camera/video!")
        fps = 0.0
        id = 1
        while True:
            t1 = time.time()
            ref, frame = capture.read()
            if not ref:
                break
            frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            frame = Image.fromarray(np.uint8(frame))
            image, _, perception = yolo.detect_image(frame,startid=id)
            id+=perception['Results'].__len__()
            frame = np.array(image)
            frame = cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)
            fps = (fps + (1. / (time.time() - t1))) / 2
            # print("fps= %.2f" % fps)
            frame = cv2.putText(frame, "fps= %.2f" % fps, (0, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
            # cv2.imshow("video", frame)
            c = cv2.waitKey(1) & 0xff
            out.write(frame)
            if c == 27:
                capture.release()
                break
        # print("Video detection done.")
        capture.release()
        # print("Save to " + video_save_path)
        out.release()
        # cv2.destroyAllWindows()
    elif mode == "fps":
        img = Image.open(fps_image_path)
        tact_time = yolo.get_FPS(img, test_interval)
        print(str(tact_time) + ' seconds, ' + str(1 / tact_time) + ' FPS, @batch_size 1')
    elif mode == "directory":
        img_names = os.listdir(dir_origin_path)
        for img_name in tqdm(img_names):
            if img_name.lower().endswith(
                    ('.bmp', '.dib', '.png', '.jpg', '.jpeg', '.pbm', '.pgm', '.ppm', '.tif', '.tiff')):
                #print(img_name,end=',')
                image_path = os.path.join(dir_origin_path, img_name)
                image = Image.open(image_path)
                r_image, _, perception = yolo.detect_image(image,img_name)
                print(json.dumps(perception))
                if not os.path.exists(dir_save_path):
                    os.makedirs(dir_save_path)
                r_image.save(os.path.join(dir_save_path, img_name), quality=95, subsampling=0)
    elif mode == "heatmap":
        while True:
            img = input('Input image filename:')
            try:
                image = Image.open(img)
            except:
                print('Open error!')
                continue
            else:
                yolo.detect_heatmap(image, heatmap_save_path)
    elif mode == "export_onnx":
        yolo.convert_to_onnx(simplify, onnx_save_path)
    else:
        raise AssertionError("Use mode: 'predict', 'video', 'fps', 'heatmap', 'export_onnx', 'dir_predict'.")


if __name__ == '__main__':
    mode=sys.argv[1]
    path=sys.argv[2]
    savepath=sys.argv[3]
    if(mode=='video'):
        fps=sys.argv[4]
        predict(mode,path,savepath,fps)
    else:
        predict(mode,path,savepath)