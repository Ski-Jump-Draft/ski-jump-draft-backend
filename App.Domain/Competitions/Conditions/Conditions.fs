namespace App.Domain.Competitions

module Conditions =
    type WindSegment = int // 0..n
    type WindMap = Map<WindSegment, float>