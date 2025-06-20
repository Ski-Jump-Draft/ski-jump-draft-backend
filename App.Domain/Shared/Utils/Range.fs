module App.Domain.Shared.Utils.Range

type OutsideRangeError<'T when 'T: comparison> =
    { Min: 'T option
      Max: 'T option
      Current: 'T }

module OutsideRangeErrorUtils =
    let inline create min max current =
        // 'T musi wspierać < i >
        if
            current < Option.defaultValue current min
            || current > Option.defaultValue current max
        then
            Some
                { Min = min
                  Max = max
                  Current = current }
        else
            None