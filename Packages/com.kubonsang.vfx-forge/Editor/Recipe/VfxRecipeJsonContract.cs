using System;
using System.Collections.Generic;
using System.Text;

namespace Kubonsang.VfxForge.Editor
{
    internal static class VfxRecipeJsonContract
    {
        private static readonly FieldRule[] FieldRules =
        {
            new FieldRule("schemaVersion", JsonValueKind.String),
            new FieldRule("id", JsonValueKind.String),
            new FieldRule("displayName", JsonValueKind.String),
            new FieldRule("template", JsonValueKind.String),
            new FieldRule("styleProfile", JsonValueKind.String),
            new FieldRule("intent", JsonValueKind.String),
            new FieldRule("outputPath", JsonValueKind.String),
            new FieldRule("anchor", JsonValueKind.String),
            new FieldRule("seed", JsonValueKind.Integer),
            new FieldRule("timing", JsonValueKind.Object),
            new FieldRule("timing.duration", JsonValueKind.Number),
            new FieldRule("timing.anticipation", JsonValueKind.Number),
            new FieldRule("timing.impact", JsonValueKind.Number),
            new FieldRule("timing.sustain", JsonValueKind.Number),
            new FieldRule("timing.decay", JsonValueKind.Number),
            new FieldRule("shape", JsonValueKind.Object),
            new FieldRule("shape.radius", JsonValueKind.Number),
            new FieldRule("shape.directionality", JsonValueKind.Number),
            new FieldRule("shape.spreadAngle", JsonValueKind.Number),
            new FieldRule("style", JsonValueKind.Object),
            new FieldRule("style.primaryColor", JsonValueKind.String),
            new FieldRule("style.secondaryColor", JsonValueKind.String),
            new FieldRule("style.emissionIntensity", JsonValueKind.Number),
            new FieldRule("style.sharpness", JsonValueKind.Number),
            new FieldRule("style.distortionStrength", JsonValueKind.Number),
            new FieldRule("layers", JsonValueKind.Array),
            new FieldRule("budget", JsonValueKind.Object),
            new FieldRule("budget.maxParticles", JsonValueKind.Integer),
            new FieldRule("budget.maxDuration", JsonValueKind.Number),
            new FieldRule("budget.maxBoundsRadius", JsonValueKind.Number),
            new FieldRule("budget.allowDistortion", JsonValueKind.Boolean),
            new FieldRule("budget.allowLight", JsonValueKind.Boolean),
            new FieldRule("capture", JsonValueKind.Object),
            new FieldRule("capture.duration", JsonValueKind.Number),
            new FieldRule("capture.frameTimes", JsonValueKind.Array),
            new FieldRule("capture.views", JsonValueKind.Array),
            new FieldRule("capture.width", JsonValueKind.Integer),
            new FieldRule("capture.height", JsonValueKind.Integer)
        };

        private static readonly string[] RequiredTopLevelFields =
        {
            "schemaVersion",
            "id",
            "template",
            "outputPath",
            "timing",
            "budget"
        };

        private static readonly string[] RequiredNestedFields =
        {
            "timing.duration",
            "budget.maxParticles",
            "budget.maxDuration"
        };

        private static readonly ArrayRule[] ArrayRules =
        {
            new ArrayRule("layers", JsonValueKind.String),
            new ArrayRule("capture.frameTimes", JsonValueKind.Number),
            new ArrayRule("capture.views", JsonValueKind.String)
        };

        public static ContractResult Validate(string json)
        {
            var reader = new JsonContractReader(json);
            if (!reader.TryRead(out JsonValueKind rootKind))
            {
                return ContractResult.Failure(
                    reader.ErrorCode,
                    reader.ErrorMessage);
            }

            if (rootKind != JsonValueKind.Object)
            {
                return ContractResult.Failure(
                    VfxRecipeErrorCodes.JsonRootType,
                    "Recipe JSON root must be an object.");
            }

            var knownFields = new Dictionary<string, FieldRule>(StringComparer.Ordinal);
            foreach (FieldRule rule in FieldRules)
            {
                knownFields.Add(rule.Path, rule);
            }

            var unknownFields = new List<string>();
            foreach (string path in reader.PropertyKinds.Keys)
            {
                if (!knownFields.ContainsKey(path))
                {
                    unknownFields.Add(path);
                }
            }

            if (unknownFields.Count > 0)
            {
                unknownFields.Sort(StringComparer.Ordinal);
                return ContractResult.Failure(
                    VfxRecipeErrorCodes.SchemaUnknownField,
                    $"Unknown JSON field: {unknownFields[0]}.");
            }

            ContractResult requiredResult = RequireFields(reader.PropertyKinds, RequiredTopLevelFields);
            if (!requiredResult.Success)
            {
                return requiredResult;
            }

            foreach (FieldRule rule in FieldRules)
            {
                if (reader.PropertyKinds.TryGetValue(rule.Path, out JsonValueKind actualKind)
                    && !KindsMatch(rule.Kind, actualKind))
                {
                    return ContractResult.Failure(
                        VfxRecipeErrorCodes.SchemaTypeMismatch,
                        $"JSON field has wrong type: {rule.Path}. Expected {Describe(rule.Kind)}.");
                }
            }

            requiredResult = RequireFields(reader.PropertyKinds, RequiredNestedFields);
            if (!requiredResult.Success)
            {
                return requiredResult;
            }

            foreach (ArrayRule rule in ArrayRules)
            {
                if (!reader.ArrayItemKinds.TryGetValue(rule.Path, out List<JsonValueKind> itemKinds))
                {
                    continue;
                }

                foreach (JsonValueKind actualKind in itemKinds)
                {
                    if (!KindsMatch(rule.ItemKind, actualKind))
                    {
                        return ContractResult.Failure(
                            VfxRecipeErrorCodes.SchemaTypeMismatch,
                            $"JSON array item has wrong type: {rule.Path}. Expected {Describe(rule.ItemKind)}.");
                    }
                }
            }

            return ContractResult.Pass();
        }

        private static ContractResult RequireFields(
            IReadOnlyDictionary<string, JsonValueKind> propertyKinds,
            IEnumerable<string> requiredFields)
        {
            foreach (string path in requiredFields)
            {
                if (!propertyKinds.ContainsKey(path))
                {
                    return ContractResult.Failure(
                        VfxRecipeErrorCodes.SchemaMissingField,
                        $"Required JSON field is missing: {path}.");
                }
            }

            return ContractResult.Pass();
        }

        private static bool KindsMatch(JsonValueKind expected, JsonValueKind actual)
        {
            return expected == actual
                || (expected == JsonValueKind.Number && actual == JsonValueKind.Integer);
        }

        private static string Describe(JsonValueKind kind)
        {
            return kind.ToString().ToLowerInvariant();
        }

        internal sealed class ContractResult
        {
            public bool Success;
            public string ErrorCode = string.Empty;
            public string ErrorMessage = string.Empty;

            public static ContractResult Pass()
            {
                return new ContractResult { Success = true };
            }

            public static ContractResult Failure(string errorCode, string errorMessage)
            {
                return new ContractResult
                {
                    Success = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage
                };
            }
        }

        private sealed class FieldRule
        {
            public readonly string Path;
            public readonly JsonValueKind Kind;

            public FieldRule(string path, JsonValueKind kind)
            {
                Path = path;
                Kind = kind;
            }
        }

        private sealed class ArrayRule
        {
            public readonly string Path;
            public readonly JsonValueKind ItemKind;

            public ArrayRule(string path, JsonValueKind itemKind)
            {
                Path = path;
                ItemKind = itemKind;
            }
        }

        private enum JsonValueKind
        {
            Object,
            Array,
            String,
            Number,
            Integer,
            Boolean,
            Null
        }

        private sealed class JsonContractReader
        {
            private readonly string json;
            private int index;

            public readonly Dictionary<string, JsonValueKind> PropertyKinds =
                new Dictionary<string, JsonValueKind>(StringComparer.Ordinal);

            public readonly Dictionary<string, List<JsonValueKind>> ArrayItemKinds =
                new Dictionary<string, List<JsonValueKind>>(StringComparer.Ordinal);

            public string ErrorCode { get; private set; } = VfxRecipeErrorCodes.JsonMalformed;
            public string ErrorMessage { get; private set; } = "Recipe JSON is malformed.";

            public JsonContractReader(string json)
            {
                this.json = json ?? string.Empty;
            }

            public bool TryRead(out JsonValueKind rootKind)
            {
                SkipWhitespace();
                if (!TryReadValue(string.Empty, out rootKind))
                {
                    return false;
                }

                SkipWhitespace();
                if (index != json.Length)
                {
                    return Malformed(out rootKind);
                }

                return true;
            }

            private bool TryReadValue(string path, out JsonValueKind kind)
            {
                SkipWhitespace();
                if (index >= json.Length)
                {
                    return Malformed(out kind);
                }

                char token = json[index];
                if (token == '{')
                {
                    return TryReadObject(path, out kind);
                }

                if (token == '[')
                {
                    return TryReadArray(path, out kind);
                }

                if (token == '"')
                {
                    if (!TryReadString(out _))
                    {
                        return Malformed(out kind);
                    }

                    kind = JsonValueKind.String;
                    return true;
                }

                if (token == '-' || IsDigit(token))
                {
                    return TryReadNumber(out kind);
                }

                if (TryReadLiteral("true") || TryReadLiteral("false"))
                {
                    kind = JsonValueKind.Boolean;
                    return true;
                }

                if (TryReadLiteral("null"))
                {
                    kind = JsonValueKind.Null;
                    return true;
                }

                return Malformed(out kind);
            }

            private bool TryReadObject(string path, out JsonValueKind kind)
            {
                kind = JsonValueKind.Object;
                index++;
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return true;
                }

                var localProperties = new HashSet<string>(StringComparer.Ordinal);
                while (index < json.Length)
                {
                    if (!TryReadString(out string propertyName))
                    {
                        return Malformed(out kind);
                    }

                    string propertyPath = string.IsNullOrEmpty(path)
                        ? propertyName
                        : $"{path}.{propertyName}";

                    if (!localProperties.Add(propertyName))
                    {
                        ErrorCode = VfxRecipeErrorCodes.SchemaDuplicateField;
                        ErrorMessage = $"Duplicate JSON field: {propertyPath}.";
                        return false;
                    }

                    SkipWhitespace();
                    if (!TryConsume(':')
                        || !TryReadValue(propertyPath, out JsonValueKind propertyKind))
                    {
                        return false;
                    }

                    PropertyKinds[propertyPath] = propertyKind;
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        return true;
                    }

                    if (!TryConsume(','))
                    {
                        return Malformed(out kind);
                    }

                    SkipWhitespace();
                }

                return Malformed(out kind);
            }

            private bool TryReadArray(string path, out JsonValueKind kind)
            {
                kind = JsonValueKind.Array;
                index++;
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return true;
                }

                if (!ArrayItemKinds.TryGetValue(path, out List<JsonValueKind> itemKinds))
                {
                    itemKinds = new List<JsonValueKind>();
                    ArrayItemKinds.Add(path, itemKinds);
                }

                while (index < json.Length)
                {
                    if (!TryReadValue($"{path}[]", out JsonValueKind itemKind))
                    {
                        return false;
                    }

                    itemKinds.Add(itemKind);
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return true;
                    }

                    if (!TryConsume(','))
                    {
                        return Malformed(out kind);
                    }

                    SkipWhitespace();
                }

                return Malformed(out kind);
            }

            private bool TryReadString(out string value)
            {
                value = string.Empty;
                if (!TryConsume('"'))
                {
                    return false;
                }

                var builder = new StringBuilder();
                while (index < json.Length)
                {
                    char character = json[index++];
                    if (character == '"')
                    {
                        value = builder.ToString();
                        return true;
                    }

                    if (character < 0x20)
                    {
                        return false;
                    }

                    if (character != '\\')
                    {
                        builder.Append(character);
                        continue;
                    }

                    if (index >= json.Length)
                    {
                        return false;
                    }

                    char escape = json[index++];
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (!TryReadUnicode(out char unicode))
                            {
                                return false;
                            }

                            builder.Append(unicode);
                            break;
                        default:
                            return false;
                    }
                }

                return false;
            }

            private bool TryReadUnicode(out char value)
            {
                value = '\0';
                if (index + 4 > json.Length)
                {
                    return false;
                }

                int code = 0;
                for (int offset = 0; offset < 4; offset++)
                {
                    int hex = HexValue(json[index++]);
                    if (hex < 0)
                    {
                        return false;
                    }

                    code = (code * 16) + hex;
                }

                value = (char)code;
                return true;
            }

            private bool TryReadNumber(out JsonValueKind kind)
            {
                kind = JsonValueKind.Integer;
                if (TryConsume('-') && index >= json.Length)
                {
                    return Malformed(out kind);
                }

                if (TryConsume('0'))
                {
                    if (index < json.Length && IsDigit(json[index]))
                    {
                        return Malformed(out kind);
                    }
                }
                else
                {
                    if (index >= json.Length || !IsDigitOneToNine(json[index]))
                    {
                        return Malformed(out kind);
                    }

                    while (index < json.Length && IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                if (TryConsume('.'))
                {
                    kind = JsonValueKind.Number;
                    if (index >= json.Length || !IsDigit(json[index]))
                    {
                        return Malformed(out kind);
                    }

                    while (index < json.Length && IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
                {
                    kind = JsonValueKind.Number;
                    index++;
                    if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                    {
                        index++;
                    }

                    if (index >= json.Length || !IsDigit(json[index]))
                    {
                        return Malformed(out kind);
                    }

                    while (index < json.Length && IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                return true;
            }

            private bool TryReadLiteral(string literal)
            {
                if (index + literal.Length > json.Length)
                {
                    return false;
                }

                for (int offset = 0; offset < literal.Length; offset++)
                {
                    if (json[index + offset] != literal[offset])
                    {
                        return false;
                    }
                }

                index += literal.Length;
                return true;
            }

            private bool TryConsume(char expected)
            {
                if (index >= json.Length || json[index] != expected)
                {
                    return false;
                }

                index++;
                return true;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length)
                {
                    char character = json[index];
                    if (character != ' ' && character != '\t' && character != '\r' && character != '\n')
                    {
                        break;
                    }

                    index++;
                }
            }

            private bool Malformed(out JsonValueKind kind)
            {
                kind = JsonValueKind.Null;
                ErrorCode = VfxRecipeErrorCodes.JsonMalformed;
                ErrorMessage = $"Recipe JSON is malformed at character {index}.";
                return false;
            }

            private static bool IsDigit(char character)
            {
                return character >= '0' && character <= '9';
            }

            private static bool IsDigitOneToNine(char character)
            {
                return character >= '1' && character <= '9';
            }

            private static int HexValue(char character)
            {
                if (character >= '0' && character <= '9') return character - '0';
                if (character >= 'a' && character <= 'f') return character - 'a' + 10;
                if (character >= 'A' && character <= 'F') return character - 'A' + 10;
                return -1;
            }
        }
    }
}
