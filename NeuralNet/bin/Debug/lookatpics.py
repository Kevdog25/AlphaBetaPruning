import matplotlib.pyplot as plt
import numpy as np
import sys

f = open('mnist_test.csv')
data = []
label = []
i = 0
for l in f:
	if int(sys.argv[1]) == i:
		line = l.split(',')
		label.append(int(line[0]))
		d = np.zeros([28,28])
		for i in range(784):
			d[i//28,i%28] = float(line[i+1])
		data.append(d)
		break
	i+=1
f.close()

plt.imshow(data[0])
plt.title(label[0])
plt.show()