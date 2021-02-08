# City Guide AR for Outdoor Sightseeing

We build a tool to enhance sightseeing with augmented reality content.
First we create a 3D model from images for a sight. Next AR content is created and fitted to the model. 
Then in real-time the AR content is superimposed precisely over the real world building.  


<p align="center">
    <img src="Localization-Pipeline/doc/Pipeline.svg" width="70%"/></a>
</p>

The work is using `hloc` in the backend, a modular toolbox for state-of-the-art 6-DoF visual localization from Sarlin 
et al. [Hloc](https://github.com/cvg/Hierarchical-Localization). 
We use it for model creation and localization. The backend functions as a HTTP server, that a run-time carries out pose estimation in real-time, matching query images against the previously created models. 

The Frontend (App) is developed with Unity and ARCore. 

We provide a step-by-step `tutorial video` for the whole process as well as a `demo video`.

## Installation

Our tool requires Python >=3.6, PyTorch >=1.1, and [COLMAP](https://colmap.github.io/index.html). 
Other dependencies are listed in `pyproject.toml` and can be installed with `poetry install`
For pose estimation, [pycolmap](https://github.com/mihaidusmanu/pycolmap) is required, which can be installed as:

```
pip install git+https://github.com/mihaidusmanu/pycolmap
```

Submodules can be pulled with `git submodule update --init --recursive`. 

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
