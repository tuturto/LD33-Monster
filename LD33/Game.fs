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

type Mob =
    { x: float32;
      y: float32;
      vx: float32;
      vy: float32;
    }

let R = System.Random()

let initialPlayerState = {x = 100.0f;
                          y = 464.0f;
                          vx = 0.0f;
                          vy = 0.0f; }

let playerStream =
    new BehaviorSubject<Mob>(initialPlayerState)

let fireStream =
    new BehaviorSubject<Mob list>([])

RxNA.Input.gameTimeStream
|> Observable.subscribe (fun time ->
    let player = playerStream.Value
    let newVY = if player.y > 464.0f && player.vy >= 0.0f then 0.0f
                    else player.vy + 12.5f * (float32)time.ElapsedGameTime.TotalSeconds
    let newY = player.y + newVY
    let newVX =
        if player.vx > 0.0f then player.vx - 1.0f * (float32)time.ElapsedGameTime.TotalSeconds
            else player.vx + 1.0f * (float32)time.ElapsedGameTime.TotalSeconds
    let newX = 
        if player.x + newVX * (float32)time.ElapsedGameTime.TotalSeconds * 50.5f < -32.0f then -32.0f
            else if player.x + newVX * (float32)time.ElapsedGameTime.TotalSeconds * 50.5f > 704.0f then 704.0f
                else player.x + newVX * (float32)time.ElapsedGameTime.TotalSeconds * 50.5f
    playerStream.OnNext {player with y = newY;
                                     vy = newVY;
                                     x = newX;
                                     vx = newVX;}
    ) |> ignore

RxNA.Renderer.renderStream
|> Observable.subscribe (fun res -> 
    if gameModeStream.Value = GameOn then
        let frame = int(res.gameTime.TotalGameTime.TotalMilliseconds / 250.0) % 4
        let texture =  match frame with 
                           | 0 -> res.textures.Item "monster_f1"
                           | 1 -> res.textures.Item "monster_f2"
                           | 2 -> res.textures.Item "monster_f3"
                           | 3 -> res.textures.Item "monster_f4"
                           | _ -> res.textures.Item ""

        let player = playerStream.Value
        res.spriteBatch.Draw(texture,
                             Vector2(player.x + 32.0f, player.y - 64.0f),
                             Color.White)) |> ignore

RxNA.Input.keysPressedStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn)
|> Observable.subscribe
    (fun x -> if x.Contains Keys.Escape then 
                    gameModeStream.OnNext MainMenuShown
              if x.Contains Keys.Space then
                    let player = playerStream.Value
                    if player.y >= 400.0f then playerStream.OnNext {player with vy = -10.0f;}
              if x.Contains Keys.Left then
                    let player = playerStream.Value
                    playerStream.OnNext {player with vx = -4.0f}
              if x.Contains Keys.Right then
                    let player = playerStream.Value
                    playerStream.OnNext {player with vx = 4.0f})
|> ignore  

RxNA.Input.keyDownStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOver)
|> Observable.subscribe
    (fun x -> match x with
                  | Keys.Escape -> gameModeStream.OnNext MainMenuShown
                  | Keys.Space -> gameModeStream.OnNext MainMenuShown
                  | _ -> ()) |> ignore



gameModeStream
|> Observable.filter (fun x -> x = GameOn)
|> Observable.subscribe
    (fun x -> fireStream.OnNext([{x=500.0f; y=350.0f; vx=10.0f; vy=0.0f}
                                 {x=700.0f; y=300.0f; vx=10.0f; vy=0.0f}
                                 {x=900.0f; y=300.0f; vx=10.0f; vy=0.0f}
                                 {x=1100.0f; y=300.0f; vx=10.0f; vy=0.0f}])) |> ignore

RxNA.Input.gameTimeStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn)
|> Observable.subscribe
    (fun gameTime -> 
        let fireSpeed = (float32)gameTime.ElapsedGameTime.TotalSeconds * 7.5f
        fireStream.OnNext (fireStream.Value |> List.map (fun fire -> {fire with x = if fire.x < -64.0f then 760.0f else fire.x - fire.vx * fireSpeed;
                                                                                y = if fire.x < -64.0f then ((float32)(R.NextDouble()) * 100.0f + 250.0f) else fire.y;
                                                                                vx = if fire.x < -64.0f then ((float32)(R.NextDouble()) * 10.0f + 5.0f) else fire.vx; }))) |> ignore

RxNA.Renderer.renderStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn)
|> Observable.subscribe
    (fun res ->
        let frame = int(res.gameTime.TotalGameTime.TotalMilliseconds / 250.0) % 4
        let texture =  match frame with 
                           | 0 -> res.textures.Item "flame_f1"
                           | 1 -> res.textures.Item "flame_f2"
                           | 2 -> res.textures.Item "flame_f3"
                           | 3 -> res.textures.Item "flame_f4"
                           | _ -> res.textures.Item ""

        fireStream.Value |> List.iter (fun fire -> res.spriteBatch.Draw(texture,
                                                                        Vector2(fire.x, fire.y),
                                                                        Color.White))
        ) |> ignore

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

        RxNA.Input.keyDownStream
        |> Observable.filter (fun x -> gameModeStream.Value = MainMenuShown)
        |> Observable.subscribe
            (fun x -> match x with
                          | Keys.Escape -> this.Exit()
                          | Keys.Space -> playerStream.OnNext(initialPlayerState)
                                          gameModeStream.OnNext(GameOn)
                          | _ -> ()) |> ignore 
 
    override this.LoadContent() =
        renderResources <-
            { graphics = this.GraphicsDevice;
              spriteBatch = new SpriteBatch(this.GraphicsDevice);
              textures = Map.empty.Add("mainmenu", contentManager.Load<Texture2D>("mainmenu"))
                                  .Add("background", contentManager.Load<Texture2D>("background"))
                                  .Add("monster_f1", contentManager.Load<Texture2D>("monster_f1"))
                                  .Add("monster_f2", contentManager.Load<Texture2D>("monster_f2"))
                                  .Add("monster_f3", contentManager.Load<Texture2D>("monster_f3"))
                                  .Add("monster_f4", contentManager.Load<Texture2D>("monster_f4"))
                                  .Add("flame_f1", contentManager.Load<Texture2D>("flame_f1"))
                                  .Add("flame_f2", contentManager.Load<Texture2D>("flame_f2"))
                                  .Add("flame_f3", contentManager.Load<Texture2D>("flame_f3"))
                                  .Add("flame_f4", contentManager.Load<Texture2D>("flame_f4"));
              gameTime = null }
 
    override this.Update (gameTime) =
        RxNA.Input.mouseStateStream.OnNext(Mouse.GetState())
        RxNA.Input.keyboardStateStream.OnNext(Keyboard.GetState())
        RxNA.Input.gameTimeStream.OnNext(gameTime)
 
    override this.Draw (gameTime) =
        RxNA.Renderer.render { renderResources with gameTime = gameTime; } |> ignore
