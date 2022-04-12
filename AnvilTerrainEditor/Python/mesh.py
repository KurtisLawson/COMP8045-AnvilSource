# Terrain Mesh Data
class MeshData:
    def __init__(self, worldPos, verts, indices):
        self.worldPos = worldPos
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
            if count < maxVerts:
                jsonString += ', '
        # End Index Loop
        jsonString += ']'

        jsonString += '}'

        return jsonString
