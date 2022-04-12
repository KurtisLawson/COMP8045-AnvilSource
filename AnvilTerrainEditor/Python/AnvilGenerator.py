from operator import index
from flask import Flask
from flask import request

import time
import sys
import math

from codecs import decode
import struct

import torch
import torch.nn as nn
import torch.optim as optim

# 
from Models import *
from Utils import *
from Generator import *

from typing import List, Tuple


def genned_data_to_vector_list(gen_data):
    # Genned data in form list of size 3074.
    vector_list = []
    return gen_data


# Default Cube - 
cubeVerts = [
    Vector3(1, 1, -1),
    Vector3(1, -1, -1),
    Vector3(1, 1, 1),
    Vector3(1, -1, 1),
    Vector3(-1, 1, -1),
    Vector3(-1, -1, -1),
    Vector3(-1, 1, 1),
    Vector3(-1, -1, 1)
]

cubeNormals = [
    
]

cubeIndices = [
    4, 2, 0,
    2, 7, 3,
    6, 5, 7,
    1, 7, 5, 
    0, 3, 1,
    4, 1, 5, 
    4, 6, 2, 
    2, 6, 7,
    6, 4, 5,
    1, 3, 7,
    0, 2, 3,
    4, 0, 1
]

def get_random_mesh(meshList, gennedMesh):
    index = random.randint(0, (len(meshList)-1))
    
    # Check for bad index, default to 0
    if index < 0 or index >= len(meshList):
        index = 0

    gennedMesh.verts = meshList[index].verts
    gennedMesh.indices = meshList[index].indices
    return gennedMesh

# Given a node, generate a mesh using the GAN.
def genIslandMesh(island, debug=True):
    if debug:

        tempFilePath = "C:/AnvilTerrainEditor/Python/IslandSet/1.obj"

        # Generated mesh
        genData = loadMeshFromFile(tempFilePath)
        genData.worldPos = island.pos
        genData.toBinary()

        return genData

    else:
        vert_generator = torch.load("vert_gen.model")
        genned_verts = vert_generator( generate_noise(1) ).tolist()

        # print("Genned Values:")
        # print(genned_verts)

        verts = []
        for step in range(0, len(genned_verts[0]), 3):
            newVec = Vector3(
                    genned_verts[0][step],
                    genned_verts[0][step+1],
                    genned_verts[0][step+2]
                )
            verts.append(newVec)
        testNewMesh = MeshData(Vector3(0,0,0), verts, cubeIndices)

        return testNewMesh

# Given a bridge, generate a mesh using the GAN
def genBridgeMesh(bridge):
    return MeshData(bridge.pos, cubeVerts, cubeIndices)

# Creates instance of the class
app = Flask(__name__)

# The route decorator tells flask what URL would trigger the function
@app.route("/Generate/", methods=['POST', 'PUT'])

def generate():
    content = request.json
    # print("Receiving", content)

    # TEMP  Load True Distribution
    labels, mesh_list = load_island_data()

    # We need to serialize the data from JSON into iterable collections of islands and bridges.
    connections = content.get("connections")
    nodes = content.get("nodes")
    
    # Place an index for each node and connection.
    start_time = time.time()

    # Iterate over every node in the graph
    print("Islands: ")
    islands = [0 for i in nodes] 
    for node in nodes:
        indx = node.get("index")

        # Create new island class instance
        newIsland = Island(
            node.get("index"),
            Vector3(node.get("pos").get("x"), 0, node.get("pos").get("y")),
            node.get("length"),
            node.get("width"),
            node.get("elevation")
        )
        
        # Set the new island to appropriate index: Order matters.
        islands[newIsland.index] = newIsland
        print(islands[indx].index, islands[indx].pos.x, islands[indx].pos.z)
    
    # Iterate over every bridge in the graph
    print("\nBridges: ")
    bridges = []
    for connection in connections:
        # Create new bridge class instance
        newBridge = Bridge(
            Vector3(connection.get("pos").get("x"), 0, connection.get("pos").get("y")),
            connection.get("outNode"),
            connection.get("inNode")
        )

        # Append the new bridge: Order doesn't matter here
        bridges.append(newBridge)

        print(newBridge.outNode, newBridge.inNode, newBridge.pos.x, newBridge.pos.z)

    # Generate a Mesh for each Island
    terrainMesh = [0 for i in nodes]

    # For each island, assign data to matching terrainMesh slot
    for island in islands:
        random_island = genIslandMesh(island, debug=True)
        random_island = get_random_mesh(mesh_list, random_island)
        terrainMesh[island.index] = random_island

    # For each bridge, append data to terrainMesh
    for bridge in bridges:
        random_bridge = genBridgeMesh(bridge)
        random_bridge = get_random_mesh(mesh_list, random_bridge)
        terrainMesh.append(random_bridge)

    # To simulate wait time on end-point, can test async operation in Unity.
    time.sleep(1)

    # Return packed JSON
    print("\nGeneration Success in", str(time.time() - start_time), "seconds, returning...")

    packedJSON = '{ "terrain" : [ '

    count = 0
    maxMeshCount = len(terrainMesh)

    # Append string for each terrain generated
    for terrain in terrainMesh:
        packedJSON += terrain.toJSON()

        count += 1
        if count < maxMeshCount:
            packedJSON += ', '

    packedJSON += ' ] }'
    # return '{ "terrain" : [ { "worldPos": { "x": 1, "y": 0, "z": 1 }, "verts" : [ {"x": 5.4, "y": 3.8, "z": 2.1 }, {"x": 1.2, "y": 0.52, "z": -2.1 } ], "indices" : [ 0, 1, 2 ] } ] }', 202
    return packedJSON, 202

# __name__ is a special variable that is used to ensure we only execute in the main file
if __name__ == "__main__":

    # Operation mode
    op_mode = ""
    if len(sys.argv) > 1:
        op_mode = str(sys.argv[1])


    if (op_mode == "train"):
        print("Training Model ... ")


    app.run(host="0.0.0.0", port=105)
    


