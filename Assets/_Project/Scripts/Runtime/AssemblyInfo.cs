using System.Runtime.CompilerServices;

// Os testes substituem serviços (por exemplo, o save em uma pasta temporária) sem setters públicos.
[assembly: InternalsVisibleTo("Ruinas.Tests.EditMode")]
[assembly: InternalsVisibleTo("Ruinas.Tests.PlayMode")]