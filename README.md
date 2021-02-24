# embeddedAFL
embeddedAFL is an integration of AFL that enables us to find vulnerabilities in industrial controllers using its SoC hardware tracing capabilities

# Acknowledgements
embeddedAFL uses the following tools to fuzz an industrial controller:

[AFL](https://github.com/google/AFL)

[MTTTY](https://github.com/bmo/mttty)

[Kelinci](https://github.com/isstac/kelinci)

For further information please see the [thesis](https://github.com/MaxWolodin/embeddedAFL/blob/main/Masterthesis_Maximilian%20Wolodin.pdf).

# Introduction
The first question when trying to fuzz an embedded device is, what should fill the gap between the fuzzer and the fuzzing target...
![Fill the gap](https://github.com/MaxWolodin/embeddedAFL/blob/main/Images/Fill_the_gap.PNG)


The solution is a software that translates between both worlds
![Sytem overview](https://github.com/MaxWolodin/embeddedAFL/blob/main/Images/System_overview.PNG)


For this setup embeddedAFL was built
![System overview embeddedAFL](https://github.com/MaxWolodin/embeddedAFL/blob/main/Images/System_overview_embeddedAFL.PNG)


Furthermore the MTTTY library was forked to be able to read and write via serial with a todays computer.
![Forked MTTTY library](https://github.com/MaxWolodin/embeddedAFL/blob/main/Images/Forked_MTTTY_library.PNG)
