module Forest

open FSharp.Control.Reactive
open Menu

type Tree =
    { texture: string;
      x: float32;
      vx: float32;
      y: float32 }

let forestTextures = ["forest_1"; "forest_2"; "forest_3"; "forest_4"; "forest_5"; "forest_6"; "forest_7"; "forest_8"]

let randomTextureName textures =
    textures |> List.item (R.Next textures.Length)

let isTreePastScreen (tree:Tree) =
    tree.x < -200.0f

let newTree() =
    { x = 800.0f;
      y = 100.0f;
      vx = 8.0f;
      texture = randomTextureName forestTextures; }

let forestStream = RxNA.Input.gameTimeStream
                   |> Observable.scanInit
                        [{x=0.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                         {x=200.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                         {x=400.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                         {x=600.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                         {x=800.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};]
                        (fun state gameTime ->
                            let forestSpeed = (float32)gameTime.ElapsedGameTime.TotalSeconds * 7.5f
                            state |> List.map (fun tree -> if isTreePastScreen tree then newTree()
                                                              else {tree with x = tree.x - tree.vx * forestSpeed}))
