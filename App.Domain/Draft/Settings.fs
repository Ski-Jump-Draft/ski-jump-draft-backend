module App.Domain.Draft.Settings

type Settings =
    { Order: Order.OrderOption
      MaxJumpersPerPlayer: uint
      UniqueJumpers: bool
      PickTimeout: Picks.PickTimeout }