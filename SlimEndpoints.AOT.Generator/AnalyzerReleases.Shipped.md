## Release 1.0

### New Rules

Rule ID | Category        | Severity | Notes
--------|-----------------|----------|--------------------
SEI001  | SlimEndpoints   | Warning  | Struct must be declared as 'readonly partial record struct'.
SEI002  | SlimEndpoints   | Warning  | Validate method must be declared as 'public static {0} Validate'.
SEI003  | SlimEndpoints   | Warning  | Converter method must be declared as 'public static AutoConverter<{0}, {1}> Converter'.