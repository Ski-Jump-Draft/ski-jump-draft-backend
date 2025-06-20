namespace App.Domain.Competition

module Conditions =
    type WindSegment = int // 0..n
    type WindMap = Map<WindSegment, float>