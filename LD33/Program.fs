open Game

[<EntryPoint>]
let main argv = 
    use g = new Game()
    g.Window.Title <- "The Monster Who Didn't Have A Name"
    g.Run()
    0 // return an integer exit code
