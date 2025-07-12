module App.Domain.Draft.Settings

type Settings =
    { Order: Order.Order
      MaxJumpersPerPlayer: uint
      UniqueJumpers: bool
      PickTimeout: Picks.PickTimeout }