module Game

open System
open System.Diagnostics
open System.Linq

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open System.Reactive.Subjects
open FSharp.Control.Reactive
open System.Reactive.Linq

open RxNA.Input
open RxNA.Renderer

open Menu

type Player =
    { x: float32;
      y: float32;
      vx: float32;
      vy: float32;
    }

let initialPlayerState = Some {x = 100.0f;
                               y = 400.0f;
                               vx = 0.0f;
                               vy = 0.0f; }

let playerStream =
    new BehaviorSubject<Player option>(None)

let playerRender res =
    if gameModeStream.Value = GameOn then
        let frame = int(res.gameTime.TotalGameTime.TotalMilliseconds / 250.0) % 2
        let texture =  match frame with 
                           | 0 -> res.textures.Item "monster_f1"
                           | 1 -> res.textures.Item "monster_f2"
                           | _ -> res.textures.Item ""

        match playerStream.Value with
            | None -> ()
            | Some player ->
                res.spriteBatch.Draw(texture,
                                     Vector2(player.x, player.y),
                                     Color.White)

let startGame() =
    playerStream.OnNext(initialPlayerState)
    gameModeStream.OnNext(GameOn)

type Game () as this =
    inherit Microsoft.Xna.Framework.Game()
 
    let graphics = new GraphicsDeviceManager(this)
    let contentManager = new ContentManager(this.Services, "Content")
    let mutable renderResources =
        { graphics = null;
          spriteBatch = null;
          textures = Map.empty;
          gameTime = null }
 
    override this.Initialize() =
        base.Initialize() |> ignore
        do graphics.PreferredBackBufferWidth <- 800
        do graphics.PreferredBackBufferHeight <- 600
        do graphics.ApplyChanges()

        gameModeStream.OnNext(MainMenuShown)

        RxNA.Renderer.renderStream |> Observable.subscribe (fun res -> playerRender res) |> ignore

        RxNA.Input.keyDownStream
        |> Observable.subscribe
            (fun x -> match x with 
                          | Keys.Escape -> 
                                match gameModeStream.Value with
                                    | MainMenuShown -> this.Exit()
                                    | GameOn -> gameModeStream.OnNext(MainMenuShown)
                                    | GameOver -> gameModeStream.OnNext(MainMenuShown)
                          | Keys.Space ->
                                match gameModeStream.Value with
                                    | MainMenuShown -> startGame()
                                    | GameOn -> () // Jump
                                    | GameOver -> () // gameModeStream.OnNext(MainMenuShown)
                          | _ -> ()) |> ignore
 
    override this.LoadContent() =
        renderResources <-
            { graphics = this.GraphicsDevice;
              spriteBatch = new SpriteBatch(this.GraphicsDevice);
              textures = Map.empty.Add("mainmenu", contentManager.Load<Texture2D>("mainmenu"))
                                  .Add("monster_f1", contentManager.Load<Texture2D>("monster_f1"))
                                  .Add("monster_f2", contentManager.Load<Texture2D>("monster_f2"));
              gameTime = null }
 
    override this.Update (gameTime) =
        RxNA.Input.mouseStateStream.OnNext(Mouse.GetState())
        RxNA.Input.keyboardStateStream.OnNext(Keyboard.GetState())
        RxNA.Input.gameTimeStream.OnNext(gameTime)
 
    override this.Draw (gameTime) =
        RxNA.Renderer.render { renderResources with gameTime = gameTime; } |> ignore
