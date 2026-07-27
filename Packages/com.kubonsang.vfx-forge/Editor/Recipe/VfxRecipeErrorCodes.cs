namespace Kubonsang.VfxForge.Editor
{
    public static class VfxRecipeErrorCodes
    {
        public const string FilePathEmpty = "RECIPE-FILE-PATH-EMPTY";
        public const string FileNotFound = "RECIPE-FILE-NOT-FOUND";
        public const string FileReadFailed = "RECIPE-FILE-READ-FAILED";
        public const string JsonEmpty = "RECIPE-JSON-EMPTY";
        public const string JsonMalformed = "RECIPE-JSON-MALFORMED";
        public const string JsonRootType = "RECIPE-JSON-ROOT-TYPE";
        public const string JsonDeserialize = "RECIPE-JSON-DESERIALIZE";
        public const string SchemaMissingField = "RECIPE-SCHEMA-MISSING-FIELD";
        public const string SchemaUnknownField = "RECIPE-SCHEMA-UNKNOWN-FIELD";
        public const string SchemaDuplicateField = "RECIPE-SCHEMA-DUPLICATE-FIELD";
        public const string SchemaTypeMismatch = "RECIPE-SCHEMA-TYPE-MISMATCH";
    }
}
