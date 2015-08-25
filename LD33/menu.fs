module Menu

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open System.Reactive.Subjects
open System.Reactive.Linq

open RxNA.Renderer

let R = System.Random()

type GameState =
    | MainMenuShown
    | GameOn
    | GameOver

let gameModeStream =
    new BehaviorSubject<GameState>(GameOn)

let menuRender res = 
    if gameModeStream.Value = MainMenuShown then
       let texture = res.textures.Item "mainmenu"
       res.spriteBatch.Draw(texture,
                            Vector2(0.0f, 0.0f),
                            Color.White)

RxNA.Renderer.renderStream
|> Observable.subscribe (fun res -> menuRender res)
|> ignore
