using System.Reflection;
using System.Reflection.Emit;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The estimate a row shows and the estimate the Estimation chart plots must come from one reading
    /// of the owner's field. These tests keep every estimate going through EstimateOf / EstimatesOf, so
    /// a second copy of "which field, and how it parses" cannot grow somewhere else.
    /// </summary>
    [TestFixture]
    public class EstimateNormalizationSeamArchUnitTest
    {
        private const string Pending = "Not built yet: the estimate on each Work Item row. Unskip one at a time while building it.";

        private static readonly Assembly ProductionAssembly = typeof(EstimateNormalizer).Assembly;

        private static readonly BindingFlags AllMemberFlags =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opCode => opCode.Value);

        // Holds today and after: if the scanner stopped seeing calls, every seam test below would pass
        // without looking at anything.
        // @us-01 @slice-01a @architecture @contract-shape:unbounded-preservation
        [Test]
        public void TheScanner_SeesACallThatIsThere()
        {
            var normalize = typeof(EstimateNormalizer).GetMethod(nameof(EstimateNormalizer.Normalize))!;
            var normalizeBatch = typeof(EstimateNormalizer).GetMethod(nameof(EstimateNormalizer.NormalizeBatch))!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(MethodCalls(normalizeBatch, normalize), Is.True);
                Assert.That(MethodCalls(normalize, normalizeBatch), Is.False);
            }
        }

        // @us-01 @slice-01a @architecture @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public void NothingOutsideTheNormalizer_NormalizesAnEstimateItself()
        {
            var lowLevel = typeof(EstimateNormalizer)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(method => method.Name is nameof(EstimateNormalizer.Normalize) or nameof(EstimateNormalizer.NormalizeBatch))
                .ToList();

            var callers = AllProductionMethods()
                .Where(method => !IsInside(method.DeclaringType, typeof(EstimateNormalizer)))
                .Where(method => lowLevel.Exists(target => MethodCalls(method, target)))
                .Select(FullNameOf)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lowLevel, Has.Count.EqualTo(2), "Normalize and NormalizeBatch must exist for this test to mean anything.");
                Assert.That(callers, Is.Empty,
                    "Every estimate must be read through EstimateNormalizer.EstimateOf or EstimatesOf, so a row and the " +
                    "chart can never disagree. Offending members: " + string.Join(", ", callers) + ".");
            }
        }

        // @us-01 @slice-01a @architecture @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public void BothEstimationChartBuilders_ReadThroughEstimatesOf()
        {
            var estimatesOf = typeof(EstimateNormalizer).GetMethod(nameof(EstimateNormalizer.EstimatesOf))!;

            var builders = typeof(BaseMetricsService)
                .GetMethods(AllMemberFlags | BindingFlags.DeclaredOnly)
                .Where(method => method.Name is "BuildEstimationVsCycleTimeResponse" or "BuildFeatureSizeEstimationResponse")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(builders, Has.Count.EqualTo(2), "Both estimation chart builders must exist for this test to mean anything.");
                foreach (var builder in builders)
                {
                    Assert.That(CallsDirectlyOrThroughItsLambdas(builder, estimatesOf), Is.True,
                        $"{builder.Name} must read the estimates through EstimateNormalizer.EstimatesOf.");
                }
            }
        }

        // @us-01 @slice-01a @architecture @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public void TheRowsEstimate_IsReadThroughEstimateOf()
        {
            var estimateOf = typeof(EstimateNormalizer).GetMethod(nameof(EstimateNormalizer.EstimateOf))!;
            var forRow = typeof(WorkItemEstimateDto).GetMethod(nameof(WorkItemEstimateDto.For))!;

            Assert.That(MethodCalls(forRow, estimateOf), Is.True,
                "WorkItemEstimateDto.For must read the estimate through EstimateNormalizer.EstimateOf.");
        }

        private static bool CallsDirectlyOrThroughItsLambdas(MethodInfo method, MethodInfo target)
        {
            if (MethodCalls(method, target))
            {
                return true;
            }

            return AllProductionMethods()
                .Where(candidate => candidate.DeclaringType is { IsNested: true } nested
                    && nested.DeclaringType == method.DeclaringType
                    && candidate.Name.Contains(method.Name, StringComparison.Ordinal))
                .Any(lambda => MethodCalls(lambda, target));
        }

        private static bool IsInside(Type? type, Type container)
        {
            for (var current = type; current != null; current = current.DeclaringType)
            {
                if (current == container)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<MethodInfo> AllProductionMethods()
        {
            return ProductionAssembly.GetTypes()
                .Where(type => !type.IsInterface)
                .SelectMany(type => type.GetMethods(AllMemberFlags | BindingFlags.DeclaredOnly));
        }

        private static string FullNameOf(MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName}.{method.Name}";
        }

        private static bool MethodCalls(MethodBase caller, MethodBase callee)
        {
            return ScanCallTargets(caller).Contains(callee);
        }

        private static List<MethodBase> ScanCallTargets(MethodBase caller)
        {
            var il = SafeGetMethodBody(caller)?.GetILAsByteArray();
            if (il is null || il.Length == 0)
            {
                return [];
            }

            var genericTypeArgs = caller.DeclaringType is { IsGenericTypeDefinition: false } declaring
                ? declaring.GetGenericArguments()
                : Type.EmptyTypes;
            var genericMethodArgs = caller is MethodInfo { IsGenericMethod: true } ? caller.GetGenericArguments() : Type.EmptyTypes;

            var targets = new List<MethodBase>();
            var offset = 0;
            while (offset < il.Length)
            {
                var opCode = ReadOpCode(il, ref offset);
                if (opCode.OperandType == OperandType.InlineMethod)
                {
                    var resolved = ResolveCallTarget(caller.Module, BitConverter.ToInt32(il, offset), genericTypeArgs, genericMethodArgs);
                    if (resolved is not null)
                    {
                        targets.Add(resolved);
                    }
                }

                offset += OperandSize(opCode.OperandType, il, offset);
            }

            return targets;
        }

        private static OpCode ReadOpCode(byte[] il, ref int offset)
        {
            short value = il[offset++];
            if (value == 0xFE)
            {
                value = (short)(0xFE00 | il[offset++]);
            }

            return OpCodesByValue[value];
        }

        private static int OperandSize(OperandType operandType, byte[] il, int offset)
        {
            return operandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, offset)),
                _ => 4,
            };
        }

        private static MethodBody? SafeGetMethodBody(MethodBase caller)
        {
            try
            {
                return caller.GetMethodBody();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static MethodBase? ResolveCallTarget(Module module, int token, Type[] genericTypeArgs, Type[] genericMethodArgs)
        {
            try
            {
                return module.ResolveMethod(token, genericTypeArgs, genericMethodArgs);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }
    }
}
