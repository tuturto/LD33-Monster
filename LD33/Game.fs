module Game

open System
open System.Diagnostics
open System.Linq
open System.Globalization

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

type Tree =
    { texture: string;
      x: float32;
      vx: float32;
      y: float32 }

let R = System.Random()

let initialPlayerState = {x = 100.0f;
                          y = 464.0f;
                          vx = 0.0f;
                          vy = 0.0f; }

let playerStream =
    new BehaviorSubject<Mob>(initialPlayerState)

let fireStream =
    new BehaviorSubject<Mob list>([])

let scoreStream = 
    new BehaviorSubject<int>(0)

let highScoreStream = 
    new BehaviorSubject<int>(0)

let forestTextures = ["forest_1"; "forest_2"; "forest_3"; "forest_4"; "forest_5"; "forest_6"; "forest_7"; "forest_8"]

let randomTextureName textures =
    textures |> List.item (R.Next textures.Length)

let forestStream =
    new BehaviorSubject<Tree list>([{x=0.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                                    {x=200.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                                    {x=400.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                                    {x=600.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};
                                    {x=800.0f; y=100.0f;vx=8.0f;texture=randomTextureName forestTextures};])

let initialTorchers = [{x=800.0f; y=464.0f; vx= 10.0f; vy=0.0f};
                       {x=750.0f; y=464.0f; vx= 11.0f; vy=0.0f};
                       {x=700.0f; y=464.0f; vx= 9.0f; vy=0.0f};
                       {x=650.0f; y=464.0f; vx= 12.0f; vy=0.0f};
                       {x=600.0f; y=464.0f; vx= 11.0f; vy=0.0f};
                       {x=800.0f; y=464.0f; vx= 10.0f; vy=0.0f};]

let torcherStream =
    new BehaviorSubject<Mob list>(initialTorchers)

RxNA.Input.gameTimeStream
|> Observable.subscribe (fun time ->
    let player = playerStream.Value
    let newVY = if player.y > 464.0f && player.vy >= 0.0f then 0.0f
                    else player.vy + 12.5f * (float32)time.ElapsedGameTime.TotalSeconds
    let newY = player.y + newVY
    let newVX =
        if player.vx > 0.0f then player.vx - 4.0f * (float32)time.ElapsedGameTime.TotalSeconds
            else player.vx + 4.0f * (float32)time.ElapsedGameTime.TotalSeconds
    let newX = 
        if player.x + newVX * (float32)time.ElapsedGameTime.TotalSeconds * 50.5f < -32.0f then -32.0f
            else if player.x + newVX * (float32)time.ElapsedGameTime.TotalSeconds * 50.5f > 704.0f then 704.0f
                else player.x + newVX * (float32)time.ElapsedGameTime.TotalSeconds * 50.5f
    playerStream.OnNext {player with y = newY;
                                     vy = newVY;
                                     x = newX;
                                     vx = newVX;}
    ) |> ignore

RxNA.Input.keysPressedStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn)
|> Observable.subscribe
    (fun x -> if x.Contains Keys.Escape then 
                    gameModeStream.OnNext MainMenuShown
              if x.Contains Keys.Space then
                    let player = playerStream.Value
                    if player.y >= 464.0f then playerStream.OnNext {player with vy = -10.0f;}
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
    (fun x -> fireStream.OnNext([{x=300.0f; y=400.0f; vx=10.0f; vy=0.0f}
                                 {x=500.0f; y=350.0f; vx=10.0f; vy=0.0f}
                                 {x=700.0f; y=300.0f; vx=10.0f; vy=0.0f}
                                 {x=900.0f; y=250.0f; vx=10.0f; vy=0.0f}
                                 {x=1100.0f; y=200.0f; vx=10.0f; vy=0.0f}])) |> ignore

let intersecting x1 y1 x2 y2 distance =
    let dx = (x1+32.f)-(x2+32.f)
    let dy = (y1-64.0f)-(y2-64.0f)
    dx*dx+dy*dy < distance*distance

let isMobPastScreen (mob:Mob) =
    mob.x < -64.0f

let newFire() = 
    { x = 864.0f + (float32)(R.NextDouble()) * 800.0f;
      y = ((float32)(R.NextDouble()) * 200.0f + 200.0f);
      vx = 10.0f;
      vy = 0.0f }

RxNA.Input.gameTimeStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn)
|> Observable.subscribe
    (fun gameTime -> 
        let fireSpeed = (float32)gameTime.ElapsedGameTime.TotalSeconds * 7.5f
        let player = playerStream.Value
        fireStream.OnNext (fireStream.Value |> List.map (fun fire -> if isMobPastScreen fire then newFire()
                                                                       else if intersecting fire.x fire.y player.x player.y 64.0f then scoreStream.OnNext(scoreStream.Value + 1)
                                                                                                                                       newFire()
                                                                           else {fire with x = fire.x - fire.vx * fireSpeed}))) |> ignore

let isTreePastScreen (tree:Tree) =
    tree.x < -200.0f

let newTree() =
    { x = 800.0f;
      y = 100.0f;
      vx = 8.0f;
      texture = randomTextureName forestTextures; }

RxNA.Input.gameTimeStream
|> Observable.subscribe
    (fun gameTime ->
        let forestSpeed = (float32)gameTime.ElapsedGameTime.TotalSeconds * 7.5f
        forestStream.OnNext (forestStream.Value |> List.map (fun tree -> if isTreePastScreen tree then newTree()
                                                                            else {tree with x = tree.x - tree.vx * forestSpeed}))) |> ignore

let newTorcher() = 
    { x = 864.0f + (float32)(R.NextDouble()) * 100.0f;
      y = 464.0f;
      vx = 8.0f + (float32)(R.NextDouble()) * 2.0f;
      vy = 0.0f }

RxNA.Input.gameTimeStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn || gameModeStream.Value = GameOver)
|> Observable.subscribe
    (fun gameTime -> 
        let mobSpeed = (float32)gameTime.ElapsedGameTime.TotalSeconds * 7.5f
        let player = playerStream.Value
        torcherStream.OnNext (torcherStream.Value |> List.map (fun mob -> if isMobPastScreen mob then newTorcher()
                                                                           else {mob with x = mob.x - mob.vx * mobSpeed}))
        torcherStream.Value |> List.iter (fun mob -> if intersecting player.x player.y mob.x mob.y 25.0f then gameModeStream.OnNext GameOver)) |> ignore

scoreStream
|> Observable.filter (fun x -> x > highScoreStream.Value)
|> Observable.subscribeObserver highScoreStream
|> ignore

RxNA.Renderer.renderStream
|> Observable.filter (fun x -> gameModeStream.Value = GameOn || gameModeStream.Value = GameOver)
|> Observable.subscribe
    (fun res ->
        List.iter (fun item -> res.spriteBatch.Draw(res.textures.Item item.texture ,
                                                    Vector2(item.x, item.y),
                                                    Color.White)) forestStream.Value) |> ignore

RxNA.Renderer.renderStream
|> Observable.subscribe
    (fun res ->
        let score = List.ofArray <| highScoreStream.Value.ToString(Globalization.CultureInfo.InvariantCulture).ToArray()
        let scoreList = List.mapi (fun index (element:char) -> (index, element.ToString(Globalization.CultureInfo.InvariantCulture))) score
        let leftmost = 795.0f - (float32)(score.Length * 64)
        List.iter (fun item -> (match item with
                                    | index, value -> res.spriteBatch.Draw(res.textures.Item value,
                                                                           Vector2((float32)index*64.0f + leftmost, 5.0f),
                                                                           Color.White))) scoreList
        ) |> ignore

RxNA.Renderer.renderStream
|> Observable.subscribe
    (fun res ->
        let score = List.ofArray <| scoreStream.Value.ToString(Globalization.CultureInfo.InvariantCulture).ToArray()
        let scoreList = List.mapi (fun index (element:char) -> (index, element.ToString(Globalization.CultureInfo.InvariantCulture))) score
        List.iter (fun item -> (match item with
                                    | index, value -> res.spriteBatch.Draw(res.textures.Item value,
                                                                           Vector2((float32)index*64.0f + 5.0f, 5.0f),
                                                                           Color.White))) scoreList
        ) |> ignore

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
                                                                        Vector2(fire.x+32.0f, fire.y-64.0f),
                                                                        Color.White))
        ) |> ignore

RxNA.Renderer.renderStream
|> Observable.subscribe (fun res -> 
    if gameModeStream.Value = GameOn || gameModeStream.Value = GameOver then
        let frame = int(res.gameTime.TotalGameTime.TotalMilliseconds / 250.0) % 4
        let texture =  match frame with 
                           | 0 -> res.textures.Item "torcher_1"
                           | 1 -> res.textures.Item "torcher_2"
                           | 2 -> res.textures.Item "torcher_3"
                           | 3 -> res.textures.Item "torcher_4"
                           | _ -> res.textures.Item ""

        torcherStream.Value |> List.iter (fun item -> res.spriteBatch.Draw(texture,
                                                                           Vector2(item.x+32.0f, item.y-64.0f),
                                                                           Color.White))) |> ignore

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
                          | Keys.Space -> playerStream.OnNext initialPlayerState
                                          torcherStream.OnNext initialTorchers
                                          scoreStream.OnNext 0
                                          gameModeStream.OnNext GameOn
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
                                  .Add("flame_f4", contentManager.Load<Texture2D>("flame_f4"))
                                  .Add("0", contentManager.Load<Texture2D>("0"))
                                  .Add("1", contentManager.Load<Texture2D>("1"))
                                  .Add("2", contentManager.Load<Texture2D>("2"))
                                  .Add("3", contentManager.Load<Texture2D>("3"))
                                  .Add("4", contentManager.Load<Texture2D>("4"))
                                  .Add("5", contentManager.Load<Texture2D>("5"))
                                  .Add("6", contentManager.Load<Texture2D>("6"))
                                  .Add("7", contentManager.Load<Texture2D>("7"))
                                  .Add("8", contentManager.Load<Texture2D>("8"))
                                  .Add("9", contentManager.Load<Texture2D>("9"))
                                  .Add("forest_1", contentManager.Load<Texture2D>("forest_1"))
                                  .Add("forest_2", contentManager.Load<Texture2D>("forest_2"))
                                  .Add("forest_3", contentManager.Load<Texture2D>("forest_3"))
                                  .Add("forest_4", contentManager.Load<Texture2D>("forest_4"))
                                  .Add("forest_5", contentManager.Load<Texture2D>("forest_5"))
                                  .Add("forest_6", contentManager.Load<Texture2D>("forest_6"))
                                  .Add("forest_7", contentManager.Load<Texture2D>("forest_7"))
                                  .Add("forest_8", contentManager.Load<Texture2D>("forest_8"))
                                  .Add("torcher_1", contentManager.Load<Texture2D>("torcher_1"))
                                  .Add("torcher_2", contentManager.Load<Texture2D>("torcher_2"))
                                  .Add("torcher_3", contentManager.Load<Texture2D>("torcher_3"))
                                  .Add("torcher_4", contentManager.Load<Texture2D>("torcher_4"));
              gameTime = null }
 
    override this.Update (gameTime) =
        RxNA.Input.mouseStateStream.OnNext(Mouse.GetState())
        RxNA.Input.keyboardStateStream.OnNext(Keyboard.GetState())
        RxNA.Input.gameTimeStream.OnNext(gameTime)
 
    override this.Draw (gameTime) =
        RxNA.Renderer.render { renderResources with gameTime = gameTime; } |> ignore
