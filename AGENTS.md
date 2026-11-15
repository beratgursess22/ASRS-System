# AGENTS.md — AS/RS Edge vs Cloud Experiment (Project Handoff)

> Put this file in the repo root (`ASRS-System/AGENTS.md`). It is the project's current context.
> The user speaks Turkish: **chat with the user in Turkish**. Code, comments, commit messages, and all academic documentation stay in **English**.

---

## 0. How to work (rules for the agent)

1. **Inspect before changing.** Read the existing code and verify the real data flow before proposing edits. Do not rewrite what already works. No full rewrites without demonstrating why they are necessary.
2. **Minimal changes.** Prefer small modifications to the existing repo over new architecture. Avoid overengineering. Two-week schedule.
3. **Do not invent.** No invented latency/CPU/RAM numbers, no invented test combinations, no assumed prices. If something is unknown, say so and ask.
4. **Ask before anything destructive, costly, or irreversible** (deleting Azure resources, resizing/creating billable resources, dropping databases, changing NSG rules to be broader, force-pushing).
5. **Secrets:** never write passwords, keys, or connection strings with passwords into the repo, logs, or chat. Use environment variables / user-secrets / a file outside git. Never print or open the SSH private key. The user holds the MySQL password.
6. **Safety:** do not open MySQL port 3306 to the internet, ever. During any Cloud API-only pilot, the serial worker must be disabled so no physical motor command can be dispatched.
7. **Use git branches** for changes (e.g. `feature/cloud-gateway`) and make small, reviewable commits.
8. **Distinguish** software-only pilot tests (synthetic UIDs, no hardware) from hardware-based experiments in every result file and in the report.
9. Explain each Azure / Linux step briefly. The user is an industrial engineering master's student, not a DevOps person.
10. Do not assume any "not yet done" item below is working.

---

## 1. Project summary

**Title:** Comparative Analysis of Edge and Cloud Computing Architectures for Automated Storage and Retrieval Systems (AS/RS).
**Context:** Academic IoT course project (Galatasaray University, INF 535), Industrial Engineering master's. Domain: IIoT, smart warehousing, edge/cloud computing.
**Main research question:** How does moving AS/RS decision-making and data processing from a local Raspberry Pi to a remote cloud server affect system performance, resource utilization, and reliability under different network conditions?

This is an **experimental comparison, not a demonstration that Edge is faster.** Results must show advantages, disadvantages, and trade-offs.

**Hybrid computing was deliberately removed.** Compare **only Edge and Cloud**. (The earlier literature-review slides still mention Hybrid / RQ4. They are outdated and the user must update the RQs.)

**Time budget:** about two weeks total. The user would like the main experiments to run within a single day once everything is ready.

---

## 2. Physical system (already exists)

- Raspberry Pi 4 (4 GB), Arduino Mega 2560, RAMPS 1.6, DRV8825 drivers, NEMA 17 steppers, MFRC522 RFID reader, limit switches, physical rack.
- Arduino does low-level motor control. The Pi handles RFID and coordinates with the backend.
- Web ERP/HMI dashboard exists. RETRIEVE can be started from the dashboard.
- **RFID reader is NOT ready/configured yet.** The first Cloud pilot uses software-generated UIDs.

Existing STORE flow: RFID tag detected → Pi reads UID → UID sent to backend → backend validates UID → rack info from MySQL → backend decides storage command → command forwarded to Arduino (serial) → Arduino drives motors → task status/result recorded.

---

## 3. Repository

GitHub: https://github.com/beratgursess22/ASRS-System (local: `C:\Users\asude\Desktop\ASRS-System`)

**Verified solution layout (from the user's directory listing):**
```
ERP-ASRS/
  ASRS.API/   (ASRS.API.csproj)   <- ASP.NET Core API host
  ASRS.BLL/
  ASRS.Core/
  ASRS.DAL/   (+ Migrations/)     <- EF Core DbContext + migrations live here
  ASRS.Web/                       <- MVC dashboard/HMI
raspberry/rfid_bridge.py          <- Pi RFID bridge (per README; verify)
```

**From the README (verify against code):** ASP.NET Core / .NET 9, MVC + Web API, EF Core, MySQL, Python scripts on the Pi, serial Pi↔Arduino. Components: `AsrsController.cs`, `AsrsSerialWorker.cs` (reads a config switch like `AsrsSerial:Enabled`, serial port such as `/dev/ttyUSB1`), `RfidUidNormalizer.cs`, `raspberry/rfid_bridge.py`.
Endpoints: `POST /api/asrs/rfid-scan`, `GET /api/asrs/commands/next`, `POST /api/asrs/commands/{id}/ack`.
The README mentions default dev credentials (e.g. a trivial DB password). Never reuse these on Azure.

**Database tables (all 27 verified on Azure):** AsrsCommands, RackCells, RfidEvents, RfidRackMaps, plus ERP tables (BillOfMaterials, CapaActions, Departments, Materials, Products, PurchaseOrderItems, PurchaseOrders, PurchaseRequestItems, PurchaseRequests, QualityDefects, QualityInspectionItems, QualityInspections, RoleClaims, Roles, SupplierItemPrices, Suppliers, UserClaims, UserLogins, UserRoles, UserTokens, Users, WorkOrders, `__EFMigrationsHistory`).
- `RackCells`: **12 seeded rows** (seed comes from migrations).
- `RfidRackMaps`: **0 rows** (no seed; test UID mappings must be added).

---

## 4. Architecture decisions

### Architecture A — Edge

Pi runs the ASP.NET Core API and a local MySQL. UID validation, DB queries, rack assignment, and command generation are all local. Commands go to Arduino over serial. No internet needed for core operation.
`RFID reader -> Raspberry Pi (local API + MySQL) -> Arduino -> AS/RS`

### Architecture B — Cloud

Pi is a thin gateway. UID is forwarded to the Azure API; validation, rack lookup, command generation, and persistence happen in Azure (MySQL on the same VM). The command is returned/polled back to the Pi, which forwards it to the Arduino and acknowledges. The Pi must NOT perform cloud-side validation or planning logic in Cloud mode, but it still handles hardware communication and device-level safety.
`RFID reader -> Pi gateway -> Azure API -> Azure MySQL`, then `Azure API -> Pi gateway -> Arduino -> AS/RS`

### Key architectural issue

`AsrsSerialWorker` may run inside the API host. A cloud API cannot reach the Arduino's USB serial port on the Pi. The queue/ack endpoints (`commands/next`, `commands/{id}/ack`) already exist, so the gateway pattern is mostly there.

### Proposed design (to verify against code, do not assume it fits)

Run the **same Pi gateway code in both modes**: it sends the UID, polls `commands/next`, writes to serial, and posts `ack`. Only the API base URL differs (Edge: `http://localhost:<port>`, Cloud: Azure). In Cloud, set `AsrsSerial:Enabled=false` on Azure. This keeps Pi-side overhead comparable and the code path identical. Document any difference the Edge in-process serial worker would introduce if the team instead keeps serial inside the API for Edge.
Retries must be **idempotent**: never execute a physical command twice because of a retried HTTP request (use command IDs / ack state).

### Database fairness

Same schema, same initial RFID mappings, same rack config, same initial occupancy. Edge uses local MySQL on the Pi. Cloud uses MySQL **on the Azure VM** (chosen over managed Azure Database for MySQL for topological equivalence with Edge and cost; document as a limitation). DB state must be reset between runs (plan: `mysqldump` of a clean baseline, restore before each run). Hardware differences (Pi 4 vs Azure VM) must be documented. CPU % across different processors is not equivalent computational performance.

### Runtime

Both sides use **.NET 9**. .NET 9 reached end of support on 10 Nov 2026 (apps still run; no new security patches). Decision: stay on .NET 9 for both Edge and Cloud for experimental consistency. Mitigate with a restrictive NSG. Do not upgrade mid-experiment. Mention in the report's limitations.

---

## 5. Experimental design (for later phases)

**Network scenarios:** (1) normal, (2) +100 ms, (3) +300 ms, (4) 5% loss, (5) 10% loss, (6) complete connection loss. Use `tc netem` on the Pi's Cloud-facing interface. Edge local API/DB (loopback) and Arduino serial must not be impaired. Define whether 100/300 ms is one-way or added RTT (egress-only delay on the Pi adds to RTT; state it explicitly). Complete loss and 10% loss can cut SSH: use an auto-revert timeout and a safe filter.

**Number of tests:** the plan says **42 tests**, but 6 scenarios x 2 architectures = 12 conditions and 42 is not divisible by 12. **Do not invent the matrix.** Ask the user how 42 was derived (possible excluded cells, e.g. some Edge conditions) and finalize repetitions and ordering with them before implementing the runner.

**Metrics:**
- Response time (request -> response), monotonic high-resolution timers.
- Server-side processing time (validation, queries, lookup, command generation, persistence) separated from network latency.
- End-to-end latency with explicit, non-mixed endpoints: API accepted / command received by Arduino / Arduino execution started / physical completion. Propose one **primary** metric (suggested: scan -> command received by Arduino) with the others supporting. Physical operation takes tens of seconds and can swamp network effects, so keep it separate.
- CPU/RAM: Pi (Edge), Pi (Cloud gateway), Azure VM. `Stopwatch` for elapsed time, `Process.TotalProcessorTime` and `WorkingSet64` for process stats, Linux tools for system stats, Azure Monitor for VM metrics. Interpret overlapping requests carefully. CPU time is not response time.
- Network traffic: bytes, request counts, bandwidth.
- Reliability: successes, failures, timeouts, missed and duplicate commands, recovery behavior, connection failures. Be careful with a command already dispatched to Arduino when the connection drops.
- Record Azure **CPU credit** metrics (B-series burstable) and avoid runs while throttled.
- Synchronize clocks (chrony/NTP) on Pi and VM, since timestamps are compared across devices.
- Store results as CSV/JSON with consistent fields. Pilot first. No assumed CPU figures (an earlier "10 ms per request" example was hypothetical).

---

## 6. Current state (what is actually DONE)

### Azure VM (verified by the user's terminal output)

- Subscription: Azure for Students (about $100 credit). Budget alert recommended at about $20.
- VM name: `vm-asrs-cloud`, region **Spain Central**, size **Standard_B2als_v2** (2 vCPU, 4 GiB), Ubuntu Server 24.04 LTS x64, no availability zone, not Spot.
- Public IP: `68.210.98.83`. SSH user: `azureuser`. SSH key auth. Private key at `%USERPROFILE%\.ssh\asrs_azure` on the user's Windows PC (never read or print it). Connect: `ssh -i $HOME\.ssh\asrs_azure azureuser@68.210.98.83`.
- Installed: ASP.NET Core **runtime 9.0.23** at `~/.dotnet` via `dotnet-install.sh`, with `DOTNET_ROOT` and `PATH` exported in `~/.bashrc`. **Runtime only, no SDK**, so `dotnet ef` cannot run on the VM. **Note:** a systemd service must set `DOTNET_ROOT=/home/azureuser/.dotnet` explicitly.
- MySQL server installed on the VM. Database `asrs_db` and user `asrs_user` created (localhost only). Password is held by the user.
- Schema applied from `asrs_schema.sql` (generated with EF Core tools 9.0.23, `--idempotent`; loaded via `sudo mysql asrs_db < ~/asrs_schema.sql`). 27 tables present, 12 RackCells, 0 RfidRackMaps.
- Network rules: SSH(22) was opened during creation (verify and restrict to the user's public IP). **No API port rule exists yet.**

### Windows dev machine

- .NET 9 SDK installed; `dotnet-ef` 9.0.23 installed as a global tool (an earlier attempt with the default latest version failed, so the 9.x version is required).
- Schema script generated with:

  ```powershell
  dotnet ef migrations script --idempotent `
    --project .\ERP-ASRS\ASRS.DAL --startup-project .\ERP-ASRS\ASRS.API -o .\asrs_schema.sql
  ```

---

## 7. NOT done yet (do not assume working)

API deployed to Azure; systemd service; API reachable from outside; NSG API rule; Pi OS/.NET/MySQL/Edge API setup; gateway implementation; RFID reader setup; test RFID mappings seeded; instrumentation/logging; test runner; netem scripts; baseline DB dump/restore; pilot; the 42-test matrix; experiments; analysis; report text; updating the literature review to remove Hybrid.

---

## 8. Next steps (ordered, critical path first)

### Step 1 — Understand the code (read-only first)

1. Read `ERP-ASRS/ASRS.API/appsettings*.json`, `Program.cs`, `Controllers/AsrsController.cs`, `AsrsSerialWorker.cs`, `RfidUidNormalizer.cs`, `raspberry/rfid_bridge.py`, `Properties/launchSettings.json`, and the entities for `RfidRackMaps`, `RackCells`, `AsrsCommands`, `RfidEvents`.
2. Report: request schema for `rfid-scan`; whether endpoints require auth (`[Authorize]`, Identity/JWT); how the connection string is read; how `AsrsSerial:Enabled` and the serial port are configured; how commands are created, queued, and acked; Kestrel URL/port binding; whether CORS/HTTPS is configured.
3. Do NOT change code until the user reviews the findings.

### Step 2 — Prepare API for Linux/Azure deployment

- Make the connection string come from an environment variable (no password in repo).
- Bind Kestrel to the chosen port on `0.0.0.0` for the VM only.
- Ensure `AsrsSerial:Enabled=false` for the Cloud configuration and verify that the serial worker does not crash when disabled.
- Add a lightweight health endpoint if none exists.

### Step 3 — Deploy the API to the VM

- Publish on Windows (framework-dependent, `linux-x64`) and `scp` to the VM.
- Create a systemd service (user `azureuser`, `DOTNET_ROOT=/home/azureuser/.dotnet`, env var for the connection string, `Restart=on-failure`). Check with `systemctl status` and `journalctl`.
- Create an NSG inbound rule for the API port with **source = user's public IP only** (add the Pi's public IP later). Restrict SSH to the user's IP. Never open 3306.

### Step 4 — API-only pilot (software, no hardware)

- Insert test rows into `RfidRackMaps` (check actual columns with `DESCRIBE`), including valid and invalid UIDs.
- Send synthetic UIDs to `POST /api/asrs/rfid-scan` from Windows (curl/Postman/a small .NET or Python client). Confirm validation, rack lookup, command creation, and persistence. Confirm **no serial/motor action is possible**.
- Label these results "software-only pilot".

### Step 5 — Pi Edge setup

Pi OS, .NET 9 runtime (same version as cloud), local MySQL with the same schema/seed, local API, serial check with the Arduino, chrony time sync, and a local STORE operation test (with the user present at the machine).

### Step 6 — Cloud gateway

Implement/extend the Pi gateway (see section 4): UID forward, command polling, serial write, ack, idempotency and failure handling. Test Cloud STORE end to end with the user at the hardware.

### Step 7 — Measurement

Timestamps at defined points (client send, API receive, processing start/end, command persisted, gateway received, written to serial, Arduino ack, physical complete), CPU/RAM samplers (Pi and VM), network byte counters, CSV output with a fixed schema, baseline RTT measurement Pi -> Azure.

### Step 8 — Network emulation and pilot

`tc netem` scripts with auto-revert, applied only to the Pi's Cloud path. Finalize the 42-test matrix **with the user**, decide order (randomize or interleave to avoid drift), take the baseline DB dump and a restore script, pilot, validate timestamps.

### Step 9 — Final experiments, analysis, and writing

Run, collect, analyze (tables, graphs, statistical summaries), document setup and limitations (hardware differences, DB co-location, .NET 9 EOL, B-series credits, RTT Istanbul <-> Spain Central).

### Cost control

Deallocate the VM when idle (`Stopped (deallocated)`, not just an OS shutdown). A static public IP may still bill. Delete the resource group when the project ends. Verify actual pricing in the Azure portal or Pricing Calculator before creating billable resources. Price "unavailable" does not mean free.

---

## 9. Open questions to ask the user

1. How was "42 tests" derived (matrix, repetitions, excluded cells)?
2. Output of `DESCRIBE RfidRackMaps; DESCRIBE AsrsCommands; SELECT * FROM RackCells;` on the VM (or run via ssh if allowed).
3. Does `rfid-scan` need authentication?
4. How are `RfidRackMaps` rows normally created (dashboard or manual)?
5. User's and the Pi's public IPs (for the NSG), and whether the Pi connects by Wi-Fi or Ethernet (relevant to netem).
6. Is the HTTP vs HTTPS choice decided for the Pi -> Azure path (must be equal for all Cloud tests, and documented)?
