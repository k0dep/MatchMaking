using OneOf;
using OneOf.Types;

namespace MatchMaking.Service.Entities;

[GenerateOneOf]
public partial class MatchSearchResponse : OneOfBase<Success, ValidationError>;