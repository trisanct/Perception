import os
import random
import sys
from xml.etree import ElementTree as ET

import numpy as np

def get_classes(classes_path):
    with open(classes_path, encoding='utf-8') as f:
        class_names = f.readlines()
    class_names = [c.strip() for c in class_names]
    return class_names, len(class_names)


classes_path = 'classes.txt'
trainval_percent = 0.9
train_percent = 0.9
VOCdevkit_path = 'VOCdevkit'
VOCdevkit_sets = [('2007', 'train'), ('2007', 'val')]
classes, _ = get_classes(classes_path)
photo_nums = np.zeros(len(VOCdevkit_sets))
nums = np.zeros(len(classes))


def convert_annotation(year, image_id, list_file):
    in_file = open(os.path.join('annotations/%s.xml' % image_id), encoding='utf-8')
    tree = ET.parse(in_file)
    root = tree.getroot()
    for obj in root.iter('object'):
        difficult = 0
        if obj.find('difficult') is not None:
            difficult = obj.find('difficult').text
        cls = obj.find('name').text
        if cls not in classes or int(difficult) == 1:
            continue
        cls_id = classes.index(cls)
        xmlbox = obj.find('bndbox')
        b = (int(float(xmlbox.find('xmin').text)), int(float(xmlbox.find('ymin').text)),
             int(float(xmlbox.find('xmax').text)), int(float(xmlbox.find('ymax').text)))
        list_file.write(" " + ",".join([str(a) for a in b]) + ',' + str(cls_id))
        nums[classes.index(cls)] = nums[classes.index(cls)] + 1


def printTable(List1, List2):
    for i in range(len(List1[0])):
        print("|", end=' ')
        for j in range(len(List1)):
            print(List1[j][i].rjust(int(List2[j])), end=' ')
            print("|", end=' ')
        print()


if __name__ == "__main__":
    random.seed(0)
    if " " in os.path.abspath(os.path.abspath("./")):
        raise ValueError("Image path cannot contain spaces.")
    print("Generate txt in ImageSets.")
    xmlfilepath = 'annotations'
    saveBasePath = 'imagesets'
    temp_xml = os.listdir(xmlfilepath)
    total_xml = []
    for xml in temp_xml:
        if xml.endswith(".xml"):
            total_xml.append(xml)
    num = len(total_xml)
    num_list = range(num)
    tv = int(num * trainval_percent)
    tr = int(tv * train_percent)
    trainval = random.sample(num_list, tv)
    train = random.sample(trainval, tr)
    ftrainval = open(os.path.join(saveBasePath, 'trainval.txt'), 'w')
    ftest = open(os.path.join(saveBasePath, 'test.txt'), 'w')
    ftrain = open(os.path.join(saveBasePath, 'train.txt'), 'w')
    fval = open(os.path.join(saveBasePath, 'val.txt'), 'w')
    for i in num_list:
        name = total_xml[i][:-4] + '\n'
        if i in trainval:
            ftrainval.write(name)
            if i in train:
                ftrain.write(name)
            else:
                fval.write(name)
        else:
            ftest.write(name)
    ftrainval.close()
    ftrain.close()
    fval.close()
    ftest.close()
    print("Generate txt in ImageSets done.")
    print("Generate 2007_train.txt and 2007_val.txt.")
    type_index = 0
    for year, image_set in VOCdevkit_sets:
        image_ids = open('imagesets/%s.txt' % image_set,
                         encoding='utf-8').read().strip().split()
        list_file = open('%s.txt' % image_set, 'w', encoding='utf-8')
        for image_id in image_ids:
            list_file.write('images/%s.jpg' %  image_id)
            convert_annotation(year, image_id, list_file)
            list_file.write('\n')
        photo_nums[type_index] = len(image_ids)
        type_index += 1
        list_file.close()
    print("Generate 2007_train.txt and 2007_val.txt done.")
    str_nums = [str(int(x)) for x in nums]
    tableData = [classes, str_nums]
    colWidths = [0] * len(tableData)
    len1 = 0
    for i in range(len(tableData)):
        for j in range(len(tableData[i])):
            if len(tableData[i][j]) > colWidths[i]:
                colWidths[i] = len(tableData[i][j])
    printTable(tableData, colWidths)
    if photo_nums[0] <= 500:
        print("The dataset is too small.")
    if np.sum(nums) == 0:
        print("model_data/voc_class.txt ERROR!")
