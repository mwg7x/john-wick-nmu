"""Encode captured in-game JPEGs, resampled using their real capture timestamps."""
from pathlib import Path
import bisect
import sys
import cv2

folder = Path(sys.argv[1])
rows = [line.split(',') for line in (folder / 'frame-times.txt').read_text().splitlines()]
times = [float(row[1]) for row in rows]
first = cv2.imread(str(folder / 'Frames' / f'{int(rows[0][0]):05}.jpg'))
height, width = first.shape[:2]
output = folder / 'movement-shooting.mp4'
writer = cv2.VideoWriter(str(output), cv2.VideoWriter_fourcc(*'mp4v'), 20, (width, height))
if not writer.isOpened():
    raise RuntimeError('Video encoder unavailable')
for sample in range(int((times[-1] - times[0]) * 20) + 1):
    index = max(0, bisect.bisect_right(times, times[0] + sample / 20) - 1)
    frame = cv2.imread(str(folder / 'Frames' / f'{int(rows[index][0]):05}.jpg'))
    writer.write(frame)
writer.release()
print(output)
