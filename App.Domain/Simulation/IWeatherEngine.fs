namespace App.Domain.Simulation

open System

type IWeatherEngine =
    abstract member GetWind : unit -> Wind
    abstract member SimulateTime : time: TimeSpan -> unit

