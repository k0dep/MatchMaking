using OneOf;
using OneOf.Types;

namespace MatchMaking.Service.Entities;

[GenerateOneOf]
public partial class MatchInfoResponse : OneOfBase<MatchInfo, NotFound, ValidationError>;