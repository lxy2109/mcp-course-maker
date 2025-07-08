from setuptools import setup, find_packages

setup(
    name="mesh_dec",
    version="0.1.1",
    description="Instant Meshes MCP Server",
    author="lxy2109",
    packages=find_packages(),
    install_requires=[
        "trimesh>=3.9.0,<4.0.0",
        "pymeshlab>=2022.2,<2024.0",
        "requests>=2.25.0,<3.0.0",
        "psutil>=5.8.0,<6.0.0",
        "fastapi>=0.70.0,<1.0.0",
        "uvicorn>=0.15.0,<1.0.0",
        "numpy>=1.19.0,<2.0.0"
    ],
    entry_points={
        "console_scripts": [
            "instant-meshes-mcp = mesh_dec.server:main"
        ]
    },
    include_package_data=True,
    python_requires=">=3.7",
)
