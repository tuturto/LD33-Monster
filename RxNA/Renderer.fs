module RxNA.Renderer

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open System.Reactive.Subjects

type TextureMap = Map<string, Texture2D>

type RenderResources = {
    graphics: GraphicsDevice;
    spriteBatch: SpriteBatch;
    textures: TextureMap;
    gameTime: GameTime }

let renderStream =
    new Subject<RenderResources>()
 
let render (res:RenderResources) =
    let texture = res.textures.Item "background"
    res.spriteBatch.Begin()
    res.spriteBatch.Draw(texture, Vector2(0.0f, 0.0f), Color.White)
    renderStream.OnNext(res)
    res.spriteBatch.End()
