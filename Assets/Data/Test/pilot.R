setwd("Documents/erf-rotation/Assets/Data/Test/")
dta <- read.table("filesP1000_Practice_erf.csv", sep = ",", header = TRUE)

dta[,c(1:18,27)] <- lapply(dta[,c(1:18,27)], FUN = function(x) { as.factor(x)})
dta$RT <- dta$EndTime - dta$BeginTime
dta$distErr <- sqrt((dta$AnsPos_X - dta$ResponsePos_X)^2 + 
  (dta$AnsPos_Z - dta$ResponsePos_Z)^2)

str(dta)

library (dplyr)

table(dta$LayoutBlockNum)
table(dta$Participant)
table(dta$LayoutType, dta$RotateDirection)

# check whether if 3 decoys messes up with the performance
# - remove false trials ...

# first trial only
dta0 <- subset(dta, dta$PairCount == "0")
# Baseline
dta_baseline <- subset(dta0, dta0$Baseline == "True")
hist(dta_baseline$RT)
hist(dta_baseline$distErr)

# rotation doesn make sense here since all conditions didn't rotate
# position erro needs to check, perhaps it's Quest's fault
dta_baseline %>% group_by(TargetType) %>%
  summarise(rt_m   = mean(RT),
            rt_sd  = sd(RT),
            err_m  = mean(distErr),
            err_sd = sd(distErr))

# testing
dta_testing <- subset(dta0, dta0$Testing == "True")
subset(dta_testing, dta_testing$TargetType == "physicalTarget")
hist(dta_testing$RT)
mean(dta_testing$RT)
sd(dta_testing$RT)
dta_testing <- dta_testing[-1,]
mean(dta_testing$RT)
sd(dta_testing$RT)

hist(dta_testing$RT)

hist(dta_testing$distErr)
mean(dta_testing$distErr)
sd(dta_testing$distErr)
dta_testing <- dta_testing[-c(1, 11),]
hist(dta_testing$distErr)

subset(dta_testing, dta_testing$TargetType == "physicalTarget")

dta_testing %>% group_by(TargetType, SelfRotation) %>%
  summarise(rt_m   = mean(RT),
            rt_sd  = sd(RT),
            err_m  = mean(distErr),
            err_sd = sd(distErr))

dta_testing %>% group_by(TargetType) %>%
  summarise(rt_m   = mean(RT),
            rt_sd  = sd(RT),
            err_m  = mean(distErr),
            err_sd = sd(distErr))

dta_testing %>% group_by(DecoyAmount, TargetType, SelfRotation) %>%
  summarise(rt_m   = mean(RT),
            rt_sd  = sd(RT),
            err_m  = mean(distErr),
            err_sd = sd(distErr))


# Observe position!

# To change
# - trialID and participantID are reversed
# - Y axis data can be removed
# - Yara's physical Target data has some error in it since her Quest reset
# - change physical target layout every 4 trials (one condition)