# ENSE Enhance Sightseeing

We build a tool to enhance sightseeing using augmented reality.
First we create a 3D model from images for a sight. Next AR content is created and fitted to the model. 
Then in real-time the AR content is superimposed precisely over the real world building.  


<p align="center">
    <img src="doc/P2.svg" width="45%"/></a>
    <img src="doc/P1.svg" width="45%"/></a>
</p>

The work is using `hloc` in the backend, a modular toolbox for state-of-the-art 6-DoF visual localization from Sarlin 
et al. [Hloc](https://github.com/cvg/Hierarchical-Localization). 
We use it for model creation and localization. The backend functions as a HTTP server, that a run-time carries out pose estimation in real-time, matching query images against the previously created models. 

The Frontend (App) is developed with Unity and ARCore and displays the content over the building.

For the whole process we provide a [Demo video](https://www.youtube-nocookie.com/embed/N2el-QiziO4) as well as a step-by-step [Tutorial video](https://www.youtube.com/embed/gFo4LCvVha8) for setting up a new sight. 


## Installation

Our tool requires Python >=3.6, PyTorch >=1.1, and [COLMAP](https://colmap.github.io/index.html). 
Other dependencies are listed in the `pyproject.toml` and can be installed using [Poetry](https://python-poetry.org/) with the command `poetry install`

## General pipeline

The backend performs 2 major tasks.

Model Creation(in advance): 
1. Extract SuperPoint local features 
2. Match features with SuperGlue
3. Build a 3D SfM model

Visual Localization (at runtime):
1. Match the query images with SuperGlue
2. Run the localization
3. Visualize and debug

The frontend.

In advance:
1. AR content is created with Gimp. 
2. Content is added to the SfM model in Unity. 
3. In unity the model is added to the `places` a list of GameObjects and the App can be redeployed. 

At runtime: 
1. The app sends HTTP request with the image to the server for localization. 
2. The answer contains the users pose, relative to the building. With this the previously created AR content is now superimposed on the building.
3. Switching between content and text based information is now available. 

## Contributions welcome!

External contributions are very much welcome. Features that are currently not implemented:

- [ ] add interface for deep image retrieval (e.g. [DIR](https://github.com/almazan/deep-image-retrieval), [NetVLAD](https://github.com/uzh-rpg/netvlad_tf_open))
- [ ] add multithreading to handle simultaneous queries
