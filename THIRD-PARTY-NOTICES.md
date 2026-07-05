# Third-party notices

Fluence.Wpf is licensed under the BSD 3-Clause License (see [LICENSE](LICENSE)). This file records third-party code adapted into Fluence.Wpf and the permissions under which it is redistributed.

---

## PSAppDeployToolkit - PSADT.ClientServer transport

**Original author / copyright holder:** Mitch Richters
**Upstream project:** PSAppDeployToolkit (PSADT), <https://github.com/PSAppDeployToolkit/PSAppDeployToolkit>
**Upstream license:** LGPL-3.0

### What is adapted

The out-of-process Fluence UI host reuses the *process-launch and pipe-framing pattern* of PSADT's `PSADT.ClientServer` transport: launching a child UI host with inherited pipe handles and exchanging length-prefixed request/response frames over anonymous pipes. The adaptation deliberately drops all SYSTEM-session machinery and the encryption layer, using Fluence's own `SpecSerialization` types.

The following files carry adapted portions and each reproduces the attribution paragraph in its header:

- `Fluence.Wpf.Specs/RemotePipeCommand.cs`
- `Fluence.Wpf.Specs/RemotePipeFrame.cs`
- `Fluence.Wpf.Specs/RemotePipeFraming.cs`
- `Fluence.Wpf.Specs/FluenceRemoteHostController.cs`
- `Fluence.Wpf.Specs.Host/Program.cs`

### Permission (relicensing grant)

Although PSAppDeployToolkit is distributed under LGPL-3.0, the `PSADT.ClientServer` code was authored by Mitch Richters, who holds its copyright, and has given explicit permission to reuse and adapt that code in Fluence.Wpf under the BSD 3-Clause License.

This grant is recorded here by the Fluence.Wpf project owner. The underlying written correspondence evidencing the grant is retained by the project owner and is available to downstream consumers or their counsel on request.

- **Grant recorded by:** Dan Cunningham
- **Date recorded:** 2026-07-04
- **Scope:** BSD-3-Clause relicensing of the adapted `PSADT.ClientServer` process-launch and pipe-framing pattern, with attribution to Mitch Richters in the reused file headers and in this notice.
