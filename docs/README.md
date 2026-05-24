# SmartRack Project Report

This folder contains the complete final report for the SmartRack AS/RS project. The report explains the mechanical design, embedded control architecture, ERP software layer, RFID integration, and industrial engineering analysis of the Automated Storage and Retrieval System prototype.

## View the Report

- [SmartRack Final Report PDF](./SmartRack_FinalReport.pdf)

## Report Information

- Title: SmartRack: a Smart Automated Storage and Retrieval System (AS/RS)
- Type: Capstone Project Final Report
- Length: 104 pages
- Date: May 2026
- Students: Asude Hazal Peker, Duygu Kudat, Ibrahim Berat Gurses
- Supervisors: Assoc. Prof. Saliha Karadayi Usta, Asst. Prof. Husamettin Osmanoglu

## Short Summary

SmartRack is a low-cost AS/RS prototype designed to reduce labor dependency, incorrect storage/retrieval, limited traceability, and unnecessary material movement in manual warehouse operations.

The system combines a 3x4 rack structure, three-axis motion mechanism, RFID-based identification, Arduino Mega and RAMPS 1.6 motor control, Raspberry Pi communication, MySQL database storage, and an ASP.NET Core MVC ERP interface.

## Report Scope

The report covers:

- Problems in manual warehouse systems and the need for AS/RS
- Literature review on AS/RS, slotting, energy, automation, and warehouse management
- SmartRack mechanical design and 3x4 rack prototype
- X, Y, and Z axis motion system
- Arduino Mega, RAMPS 1.6, DRV8825, NEMA 17 motors, and limit switches
- Raspberry Pi and MFRC522 RFID reader integration
- ERP, REST API, MySQL database, and command queue architecture
- STORE and RETRIEVE workflows
- Industrial engineering calculations and project results

## Hardware and Software Overview

The mechanical prototype is 130 cm long, 100 cm high, and 25 cm deep. The rack contains 4 columns and 3 rows, giving 12 storage cells. The shelf spacing is planned as 25 cm horizontally and vertically.

Motion system:

- X axis: horizontal rack positioning
- Z axis: vertical row positioning
- Y axis: pushing a package into a cell or retrieving it from a cell

Control system:

- Arduino Mega 2560 handles low-level stepper motor control.
- RAMPS 1.6 and DRV8825 drivers control the motors.
- Raspberry Pi handles RFID scanning and upper-level communication.
- ERP/API layer creates commands, stores database records, and monitors the system.

## Industrial Engineering Analyses

The report evaluates both the technical prototype and its industrial feasibility.

### ABC-Pareto Slotting Analysis

ABC analysis was used to optimize rack assignment according to item retrieval frequency.

Main results:

- Class A items represent the highest operational demand.
- Segment A generated 18,875 demand units.
- Segment B generated 5,710 demand units.
- Segment C generated 1,100 demand units.
- High-frequency Class A items should be assigned to cells closer to the input/output station.

This approach aims to reduce travel distance, operation time, and unnecessary energy consumption.

### AHP Performance Evaluation

AHP was used to compare SmartRack with a conventional forklift-based warehouse approach.

Evaluation criteria:

- Retrieval and storage speed
- Accuracy
- Safety
- Cost efficiency
- Energy efficiency

The report concludes that retrieval/storage speed and safety are among the most critical criteria. The consistency ratio stayed below the acceptable 0.10 threshold, so the AHP comparisons were considered consistent.

### Cost-Benefit and Payback Analysis

The cost-benefit and payback analysis considered CAPEX, OPEX, labor requirements, maintenance cost, operational savings, and long-term feasibility.

Key findings:

- SmartRack provides lower annual operating cost than a forklift-based structure.
- Automation reduces labor dependency.
- Labor requirement can be reduced from 10 operators to 2 operators, corresponding to approximately 80% labor savings.
- The Benefit-Cost Ratio is greater than 1.
- The estimated payback period is approximately 2 years.

### Carbon Footprint Analysis

The carbon footprint analysis compares LPG forklift, electric forklift, and Smart AS/RS alternatives over a 10-year operating period.

Results:

- LPG forklift: 428.56 tons CO2
- Electric forklift, simple calculation: 140.24 tons CO2
- Electric forklift with battery/charging losses: 311.64 tons CO2
- Smart AS/RS direct operational carbon footprint: 39.62 tons CO2
- Smart AS/RS partial total including heating effect: 189.36 tons CO2

These results show that Smart AS/RS has the lowest direct operational carbon footprint among the compared alternatives.

### FMEA Risk Analysis

FMEA was used to prioritize possible failure modes. Risk Priority Number (RPN) was calculated using Severity, Occurrence, and Detection values.

Most critical risks:

- Inaccurate object placement: RPN 100
- Z-axis / shuttle movement problem: RPN 100
- Controller communication problem: RPN 54
- Rack structural stability: RPN 50
- X and Y axis motion-control risks: RPN 48

The report identifies calibration, limit-switch control, movement testing, and regular mechanical inspection as priority improvement areas.

## Testing and Results

The report shows that the SmartRack prototype successfully performed autonomous storage and retrieval operations at prototype scale.

Validated areas:

- Mechanical rack and shuttle movement
- Arduino Mega, RAMPS 1.6, and DRV8825 motor control
- X/Z homing mechanism
- RFID UID reading and ERP mapping
- Limit-switch feedback
- STORE and RETRIEVE command workflows
- Rack occupancy tracking through the ERP dashboard
- MySQL database and command queue
- REST API system integration

## Limitations

The report also notes several prototype limitations:

- The system was not tested under full industrial heavy-load and continuous-operation conditions.
- Vibration on the Y axis, slight downward displacement on the Z axis, and belt skipping were observed in some tests.
- The system was tested in a local and controlled network environment.
- AI-based route optimization, predictive maintenance, and advanced inventory forecasting were not implemented in this scope.
- Industrial calculations depend on prototype-scale assumptions and limited long-term operational data.

## Future Improvements

Suggested future work includes:

- Stronger mechanical structure and industrial-grade motors
- Higher load-capacity rack system
- Separate entry and exit points
- Conveyor-assisted item input/output
- Additional sensors for the Y axis and boundary positions
- PLC-based industrial control architecture
- Simultaneous X/Z axis movement
- Camera and image-processing-based rack occupancy validation
- Cloud-based monitoring and remote access
- AI-based slotting, route optimization, and predictive maintenance

## Conclusion

The SmartRack report shows that the project is not only a working technical prototype, but also a measurable warehouse automation solution from operational, economic, environmental, and risk-management perspectives. The system combines RFID, embedded automation, ERP-supported inventory management, and analytical decision-making methods in one Industry 4.0-oriented prototype.
