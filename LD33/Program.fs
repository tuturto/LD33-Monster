open Game

[<EntryPoint>]
let main argv = 
    use g = new Game()
    g.Window.Title <- "You are the Monster"
    g.Run()
    0 // return an integer exit code
