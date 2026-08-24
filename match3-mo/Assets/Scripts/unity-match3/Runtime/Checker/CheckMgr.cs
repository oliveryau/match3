using System.Collections.Generic;

namespace Match3
{
    /// <summary>
    /// Port of CheckMgr. Checker order matches CHECK_DOWN_LIST plus special-swap checkers first.
    /// Excel CheckItemModel is not in the dump, so this is the reconstructed default table.
    /// </summary>
    public class CheckMgr
    {
        readonly FinderMgr _finder;
        readonly List<CheckerDef> _all = new List<CheckerDef>();
        readonly List<CheckerDef> _drop = new List<CheckerDef>();

        public CheckMgr(FinderMgr finder)
        {
            _finder = finder;
            BuildDefaultTable();
        }

        void BuildDefaultTable()
        {
            int id = 1;
            CheckerDef Add(CheckerDef d)
            {
                d.Id = id++;
                _all.Add(d);
                return d;
            }

            // Special + special (paramCount 2) — run before shape matches.
            Add(new CheckerDef
            {
                Name = "TwoLightBall",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.LightBall },
                TypesB = new[] { ObjectType.LightBall },
                SpawnSpecial = SpecialType.None
            });
            Add(new CheckerDef
            {
                Name = "LightBallAndOther",
                Kind = CheckerKind.SwapOneIsType,
                ParamCount = 2,
                TypesA = new[] { ObjectType.LightBall }
            });
            Add(new CheckerDef
            {
                Name = "TwoMissile",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.HMissile, ObjectType.VMissile },
                TypesB = new[] { ObjectType.HMissile, ObjectType.VMissile }
            });
            Add(new CheckerDef
            {
                Name = "TwoPowderKeg",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.PowderKeg },
                TypesB = new[] { ObjectType.PowderKeg }
            });
            Add(new CheckerDef
            {
                Name = "TwoPropeller",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.Propeller },
                TypesB = new[] { ObjectType.Propeller }
            });
            Add(new CheckerDef
            {
                Name = "MissileAndKeg",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.HMissile, ObjectType.VMissile },
                TypesB = new[] { ObjectType.PowderKeg }
            });
            Add(new CheckerDef
            {
                Name = "PropellerAndMissile",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.Propeller },
                TypesB = new[] { ObjectType.HMissile, ObjectType.VMissile }
            });
            Add(new CheckerDef
            {
                Name = "PropellerAndKeg",
                Kind = CheckerKind.SwapPair,
                ParamCount = 2,
                TypesA = new[] { ObjectType.Propeller },
                TypesB = new[] { ObjectType.PowderKeg }
            });
            Add(new CheckerDef
            {
                Name = "ActivateSpecial",
                Kind = CheckerKind.IsType,
                ParamCount = 1,
                TypesA = new[]
                {
                    ObjectType.HMissile, ObjectType.VMissile,
                    ObjectType.Propeller, ObjectType.PowderKeg, ObjectType.LightBall
                }
            });

            // Shape matches — same order as CHECK_DOWN_LIST.
            void Shape(string name, FinderId[] finders, int min, SpecialType spawn, bool dropList)
            {
                var d = Add(new CheckerDef
                {
                    Name = name,
                    Kind = CheckerKind.FinderGroup,
                    ParamCount = 1,
                    Finders = finders,
                    MinCount = min,
                    SpawnSpecial = spawn
                });
                if (dropList) _drop.Add(d);
            }

            Shape("FiveLine", new[] { FinderId.SameTypeH, FinderId.SameTypeV }, 5, SpecialType.LightBall, true);
            Shape("TShape", new[] { FinderId.TWord }, 5, SpecialType.PowderKeg, true);
            Shape("LShape", new[] { FinderId.LShape }, 5, SpecialType.PowderKeg, true);
            Shape("FourH", new[] { FinderId.SameTypeH }, 4, SpecialType.HMissile, true);
            Shape("FourV", new[] { FinderId.SameTypeV }, 4, SpecialType.VMissile, true);
            Shape("Square", new[] { FinderId.TianWord }, 4, SpecialType.Propeller, true);
            Shape("Three", new[] { FinderId.SameTypeH, FinderId.SameTypeV }, 3, SpecialType.None, true);
        }

        public CheckCellResult CheckStartForCells(List<Cell> list, FinderCheckType check)
        {
            foreach (var def in _all)
            {
                if (list.Count != def.ParamCount) continue;
                // Skip the dummy "ActivateSpecial" for swap of two normals.
                var r = Checkers.Run(def, list, _finder, check);
                if (r != null) return r;
            }
            return null;
        }

        public CheckCellResult CheckDrop(List<Cell> list, FinderCheckType check)
        {
            foreach (var def in _drop)
            {
                var r = Checkers.Run(def, list, _finder, check);
                if (r != null) return r;
            }
            return null;
        }
    }
}
