namespace App.Domain.Simulating

type Context = Context of byte[]

type ISimulator =
    abstract member Simulate: Context -> Jump
