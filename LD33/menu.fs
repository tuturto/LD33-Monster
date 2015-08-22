module Menu

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open RxNA.Renderer

type GameState =
    | MainMenuShown
    | GameOn
    | GameOver

let menuRender res =
    let texture = res.textures.Item "mainmenu"
    res.spriteBatch.Draw(texture,
                         Vector2(0.0f, 0.0f),
                         Color.White)

let MainMenuInit =
    RxNA.Renderer.renderStream
    |> Observable.subscribe (fun res ->
        menuRender res)
    |> ignore

