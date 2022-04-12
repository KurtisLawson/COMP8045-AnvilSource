Environment Req.
	Unity v2020.3.27f1
	Python v3.8.6

Major Python Library Dependencies:
	PyTorch
	Flask
	Matlabplot
	Numpy

USER MANUAL

CRUCIAL - The AnvilTerrainGenerator folder must be placed in the C:/ drives root.
			The code makes use of the fixed code path C:/AnvilTerrainGenerator

Opening the Anvil Editor requires Unity v2020.3.27f1
	
	Opening the project will reveal a simple scene with a single
	terrain feature rendered in-engine.
	
	Accessing Window > AnvilEditor will open the node editor.
	
	The following commands are possible in the editor:
	
		Create Node - Right Click > Add Node
		Delete Node - Left Click Node > Right Click > Remove Node
		Create Link - Left Click Primary Node Edge > Left Click Secondary Node Edge
		Delete Link - Left Click White Handle at center of Link

	Clicking "Generate Terrain" will send a request to the Python Endpoint.
	This must be running locally.
	
To activate the virtual python endpoint, open a command line
in the C:\AnvilTerrainEditor folder, and run the commands:

	anvil-venv\Scripts\activate.bat
	cd Python
	python AnvilGenerator.py

To view the model training loop on a simplified problem domain, run:

	python Generator.py