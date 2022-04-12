from os import listdir
from os.path import isfile, join

from codecs import decode
import struct

from typing import List, Tuple

# BINARY FUNCTIONS
def int_to_bytes(n, length):  # Helper function
    """ Int/long to byte string.

        Python 3.2+ has a built-in int.to_bytes() method that could be used
        instead, but the following works in earlier versions including 2.x.
    """
    return decode('%%0%dx' % (length << 1) % n, 'hex')[-length:]

def float_to_bin(value) -> str:
    [d] = struct.unpack(">Q", struct.pack(">d", value))
    return '{:064b}'.format(d)

def bin_to_float(value) -> float:
    bf = int_to_bytes(int(value, 2), 8)  # 8 bytes needed for IEEE 754 binary64.
    return struct.unpack('>d', bf)[0]

def int_to_bin(x: int) -> str:
    return bin(x)[2:].zfill(16)
    
def bin_to_int(value) -> int:
    return int(str(value),2)

def parseVertEntry(entry):
    return Vector3( float(entry[1]), float(entry[2]), float(entry[3]) )

def parseIndexEntry(entry):
    index1 = entry[1].split('/')[0]
    index2 = entry[2].split('/')[0]
    index3 = entry[3].split('/')[0]
    return Vector3( int(index1)-1, int(index2)-1, int(index3)-1 )

# Input functions
def bin_to_list(value) -> List[int]:
    # Iterate over index
    b_list = []
    for i in range(0, len(str(value))):
        b_list.append(int(value[i]))
    
    return b_list

def fill_vert_bin_list(list, bits, max_size) -> List[ List[int] ]:
    num_to_fill = max_size - len(list)

    l = [0] * bits
    for i in range(0, num_to_fill):
        list.append(l) 

    return list

def fill_vert_list(list, max_size):
    num_to_fill = max_size - len(list)

    for i in range(0, num_to_fill):
        list.append(0.0)

    return list


# Object to store incoming island data
class Island:
    def __init__(self, index, pos, length, width, elevation):
        self.index = index
        self.pos = pos
        self.length = length
        self.width = width
        self.elevation = elevation

# Object to store island pairs
class Bridge:
    def __init__(self, pos, outNode, inNode):
        self.pos = pos
        self.outNode = outNode
        self.inNode = inNode

# Vector class
class Vector3:
    def __init__(self, x, y, z):
        self.x = x
        self.y = y
        self.z = z

    def toJSON(self):
        jsonString = '{'
        # { "x": 1, "y": 0, "z": 1 }
        jsonString += ('"x":' + str(self.x) + ',"y":' + str(self.y) + ',"z":' + str(self.z))
        jsonString += '}'

        return jsonString

# Terrain Mesh Data
class MeshData:
    
    def __init__(self, worldPos, verts, indices):
        self.MAX_VERTS = 1024

        self.worldPos = worldPos
        self.verts = verts
        self.indices = indices

        # Float collections
        self.verts_vector = []
        self.x_channel = []
        self.y_channel = []
        self.z_channel = []

        # Binary collections
        self.vert_bin = []
        self.vert_list_bin = []
        self.index_bin = []

        self.x_bin_channel = []
        self.y_bin_channel = []
        self.z_bin_channel = []

    
    def toBinary(self):
        self.vert_bin = []
        self.vert_list_bin = []
        self.index_bin = []

        self.x_bin_channel = []
        self.y_bin_channel = []
        self.z_bin_channel = []

        # Part one: Create vert array
        for vert in self.verts:
            # Assign the vert to a string
            bin_x = float_to_bin(vert.x)
            bin_list_x = bin_to_list(bin_x)
            bin_y = float_to_bin(vert.y)
            bin_list_y = bin_to_list(bin_y)
            bin_z = float_to_bin(vert.z)
            bin_list_z = bin_to_list(bin_z)

            # print('\nNEW VERT: ')
            # print('\tX : ' + str(vert.x))
            # print('\tX : ' + bin_x)
            # print('\tX : ' + str(bin_list_x))
            # print('\n\tY : ' + str(vert.y))
            # print('\tY : ' + bin_y)
            # print('\tY : ' + str(bin_list_y))
            # print('\n\tZ : ' + str(vert.z))
            # print('\tZ : ' + bin_z)
            # print('\tZ : ' + str(bin_list_z))

            # Add the new string to our binary string
            self.vert_bin.append(bin_x)
            self.verts_vector.append(vert.x)
            self.x_channel.append(vert.x)
            self.x_bin_channel.append( bin_list_x )
            self.vert_list_bin.append( bin_list_x )

            self.vert_bin.append(bin_y)
            self.verts_vector.append(vert.y)
            self.y_channel.append(vert.y)
            self.y_bin_channel.append( bin_list_y )
            self.vert_list_bin.append( bin_list_y )

            self.vert_bin.append(bin_z)
            self.verts_vector.append(vert.z)
            self.z_channel.append(vert.z)
            self.z_bin_channel.append( bin_list_z )
            self.vert_list_bin.append( bin_list_z )
        
        # Once the verts are filled, we equalize the tensor size
        self.x_bin_channel = fill_vert_bin_list(self.x_bin_channel, 64, self.MAX_VERTS)
        self.x_channel = fill_vert_list(self.x_channel, self.MAX_VERTS)

        self.y_bin_channel = fill_vert_bin_list(self.y_bin_channel, 64, self.MAX_VERTS)
        self.y_channel = fill_vert_list(self.y_channel, self.MAX_VERTS)

        self.z_bin_channel = fill_vert_bin_list(self.z_bin_channel, 64, self.MAX_VERTS)
        self.z_channel = fill_vert_list(self.z_channel, self.MAX_VERTS)

        self.vert_list_bin = fill_vert_bin_list(self.vert_list_bin, 64, self.MAX_VERTS*3)
        self.verts_vector = fill_vert_list(self.verts_vector, self.MAX_VERTS*3)

        # Part two: Create index array
        for index in self.indices:
            bin_index = int_to_bin(index)
            # print('NEW INDEX: ')
            # print('\n\tI : ' + bin_index)
            self.index_bin.append(bin_index)


    def fromBinary(self, vert_bin, index_bin):
        verts = []
        indices = []
        for step in range(0, len(vert_bin), 3):
            newVec = Vector3(
                    bin_to_float(vert_bin[step]),
                    bin_to_float(vert_bin[step+1]),
                    bin_to_float(vert_bin[step+2])
                )
            verts.append(newVec)

        for index in index_bin:
            indices.append( bin_to_int(index) )

        self.verts = verts
        self.indices = indices

    def toJSON(self):
        jsonString = '{'
        # "worldPos": { "x": 1, "y": 0, "z": 1 },
        jsonString += '"worldPos":' + self.worldPos.toJSON() + ', '

        # "verts" : [ {"x": 5.4, "y": 3.8, "z": 2.1 }, {"x": 1.2, "y": 0.52, "z": -2.1 } ],
        count = 0
        maxVerts = len(self.verts)

        jsonString += '"verts":['

        # Start Vert Loop
        for vert in self.verts:
            jsonString += vert.toJSON()

            count += 1
            if count < maxVerts:
                jsonString += ', '
        # End Vert Loop
        jsonString += '], '


        # "indices" : [ 0, 1, 2 ]
        count = 0
        maxIndices = len(self.indices)

        jsonString += '"indices":['

        # Start Index Loop
        for index in self.indices:
            jsonString += str(index)

            count += 1
            if count < maxIndices:
                jsonString += ', '
        # End Index Loop
        jsonString += ']'

        jsonString += '}'

        return jsonString

def iswavefront(filename):
    file_split = filename.split(sep=".")
    return file_split[1] == "obj"

# Given a file path, return a mesh data
def loadMeshFromFile(filePath):
    parsedVerts = []
    parsedIndices = []

    # Using readlines()
    objFile = open(filePath, 'r')
 
    while True:
        # Get next line from file
        line = objFile.readline()

        # if line is empty
        # end of file is reached
        if not line:
            break
        
        # 1. Itemize the string
        entry = line.split(' ')

        # 2. Get the first character of the line.
        lineType = entry[0]

        # 3. If v -> entry[1] - entry[4]
        if lineType == 'v':
            parsedVert = parseVertEntry(entry)

            parsedVerts.append( parsedVert )

        # 3. If t -> entry[1] - entry[4]
        elif lineType == 'f':
            parsedIndexGroup = parseIndexEntry(entry)
            parsedIndices.append( parsedIndexGroup.x )
            parsedIndices.append( parsedIndexGroup.y )
            parsedIndices.append( parsedIndexGroup.z )

        else:
            pass

    mesh = MeshData(Vector3(0,0,0), parsedVerts, parsedIndices)
    mesh.toBinary()
    return mesh

# Load the training set from the folder, and generate a training set
#       Tuple[0] ( label : 1  ||  vert(64) : [ [ 1, 0, 1, 1, 0, 1, 0, 0  ... ], [ 1, 0, 1, 1, 0, 1, 0, 0  ... ] ]  || index(16) : [ [ 0, 0, 1, 1, ... ], [ 0, 0, 1, 1, ... ] ] )
#       batch_size : How many total inputs are loaded.
def load_island_data() -> Tuple[ List[int], List[ MeshData ] ]:
    tempFilePath = "C:/AnvilTerrainEditor/Python/IslandSet/"

    print("Loading Terrain Data ...")
    # # Get the number of binary places needed to represent the maximum number
    max_length = 64

    # # Sample batch_size number of integers in range 0-max_int
    # # sampled_integers = np.random.randint(0, int(max_int / 2), batch_size)
    loaded_islands = []

    # Get all file names from the directory.
    onlyfiles = [f for f in listdir(tempFilePath) if (isfile(join(tempFilePath, f)))]

    # Iterate through each filepath
    for file in onlyfiles:

        if not iswavefront(file):
            continue

        newIsland = loadMeshFromFile(tempFilePath+file)
        island_verts = newIsland.vert_list_bin # List [ List[int] ], single channel
        
        loaded_islands.append(newIsland)

    # # create a list of labels all ones because all numbers are even
    labels = [1] * len(loaded_islands)

    # # Generate a list of binary numbers for training.
    # data = [create_binary_list_from_int(int(x * 2)) for x in sampled_integers]
    # data = [([0] * (max_length - len(x))) + x for x in data]

    return labels, loaded_islands