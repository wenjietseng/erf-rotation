# TODOs in Unity
# - Warning: directionTable and assigning direction are wrong:
#   turning left and right is not balanced across each layout.
#   Check PrepareCondition and Line 576-606
# - Warning: Need to add both target positions so that we can check answers
# - calibratedDistance --> UnityDistance
# - file names set up
# - trialID and participantID are reversed
# - Y axis data can be removed
# - LayoutBlockNum can be removed
# - move TargetName to the other factor variables

## get pilot data
# setwd("Documents/erf-rotation/Assets/Data/Test/")

## load packages
library(Rmisc) # plyr and dplyr imcompatible. Need to look into code implementation
library(dplyr)
library(ggplot2)

# y
# conditions and layouts (5,6,7) have some mistakes. Need to check that
dta_y <- read.table("1003/filesP1003_erf.csv", sep=",", h=T)
dta_q_y <- read.table("1003/filesP1003_questionnaire.csv", sep = ",", h=T)
dta_c_y <- read.table("1003/filesP1003Calibration_erf.csv", sep = ",", h=T)

# wj
dta_w <- read.table("1002/filesP1002_erf.csv", sep=",", h=T)
dta_q_w <- read.table("1002/filesP1002_questionnaire.csv", sep = ",", h=T)
dta_c_w <- read.table("1002/filesP1002Calibration_erf.csv", sep = ",", h=T)

# j
dta_j <- read.table("1001/filesP1001_FormalStudy_erf.csv", sep=",", h=T)
dta_q_j <- read.table("1001/filesP1001_questionnaire.csv", sep=",", h=T)
dta_c_j <- read.table("1001/filesP1001Calibration_erf.csv", sep=",", h=T)

## quickly organize data
# Calibration ratio
dta_c_j <- dta_c_j[1,]
dta_c_j$ParticipantID <- as.numeric(dta_c_j$ParticipantID)
dta_c_j$CalibratedRatio <- as.numeric(dta_c_j$CalibratedRatio)

dta_c <- rbind(dta_c_j, dta_c_w, dta_c_y)
dta_c$ParticipantID <- as.factor(dta_c$ParticipantID)
names(dta_c)[2] <- "UnityDistance"
dta_c$CalibratedRatio <- 1.5 / dta_c$UnityDistance
dta_c

# Questionnaire
dta_q <- rbind(dta_q_j, dta_q_w, dta_q_y)
dta_q$ParticipantID <- as.factor(dta_q$ParticipantID)
dta_q$Item <- as.factor(dta_q$Item)

ggplot(dta_q, aes(x = Item, y = Response)) +
  geom_boxplot(fill = "gray90") +
  lims(y = c(1, 5)) +
  coord_flip() +
  theme_bw()

tapply(dta_q$Response, dta_q$Item, mean)
tapply(dta_q$Response, dta_q$ParticipantID, mean)

# Experiment data
dta_w <- subset(dta_w, dta_w$isPractice == "False")
dta_w <- dta_w[,-3] # remove isPractice
dta_y <- subset(dta_y, dta_y$isPractice == "False")
dta_y <- dta_y[,-3] # remove isPractice

## this data is with mountain
dta_pilot <- rbind(dta_j, dta_w, dta_y) 
dim(dta_pilot)
names(dta_pilot)[1] <- "Participant"
names(dta_pilot)[2] <- "trialID"

dta_pilot[,c(1:18,27)] <- lapply(
  dta_pilot[,c(1:18,27)], FUN = function(x) { as.factor(x)})

# add RT and position error
dta_pilot$RT <- dta_pilot$EndTime - dta_pilot$BeginTime
dta_pilot$distErr <- sqrt((dta_pilot$AnsPos_X - dta_pilot$ResponsePos_X)^2 + 
                      (dta_pilot$AnsPos_Z - dta_pilot$ResponsePos_Z)^2)

str(dta_pilot) # data is loaded for further processing

## Only the first response counts. 
dta0 <- subset(dta_pilot, dta_pilot$PairCount == "0")
dta1 <- subset(dta_pilot, dta_pilot$PairCount == "1")

# copy past Ans target name and position
dim(dta0)
dim(dta1)

dta0$OtherTargetPos_X <- dta1$AnsPos_X
dta0$OtherTargetPos_Z <- dta1$AnsPos_Z
dta0$OtherTargetName <- dta1$TargetName
dta0$distTargets <- sqrt((dta0$OtherTargetPos_X - dta0$ResponsePos_X)^2 + 
                          (dta0$OtherTargetPos_Z - dta0$ResponsePos_Z)^2)
str(dta0)
## Remove decoy responses
dta0 <- subset(dta0, dta0$DecoyBaseline == "False")
dta0 <- subset(dta0, dta0$DecoyTesting == "False")
dta0[, c(32,36)]

levels(dta0$Baseline) <- c("Testing", "Baseline")
##########################################
# check if turning direction is balanced, at the moment it's wrong
# need to change in Unity
##########################################
head(dta0, n = 20)
table(dta0$LayoutType, dta0$RotateDirection, dta0$Participant)

## Remove outliers by each participant
# https://stackoverflow.com/questions/67684451/apply-function-to-each-level-of-a-factor-participant-to-remove-outliers-based
# https://neuraldatascience.io/5-eda/data_cleaning.html

# Use z score and set the bar to 2.5
dta0 <- dta0 |> group_by(Participant) |>
  mutate(OutlierRT = RT >= mean(RT) + (2.5 * sd(RT)) | RT <= mean(RT) - (2.5 * sd(RT)),
         OutlierDist = distErr >= mean(distErr) + (2.5 * sd(distErr)) | distErr <= mean(distErr) - (2.5 * sd(distErr)))

dta_clean <- dta0[-c(which(dta0$OutlierDist == TRUE), which(dta0$OutlierRT == TRUE)),]

# removed portion
1 - (dim(dta_clean)[1]/dim(dta0)[1])

# specifically for one pilot data
idx.p <- which(dta_clean$Participant == "P1003")
idx.cond <- which(dta_clean$ConditionBlockNum == 5 | dta_clean$ConditionBlockNum == 6 | dta_clean$ConditionBlockNum == 7)
idx.phytar <- which(dta_clean$TargetType == "physicalTarget")
dta_clean2 <- dta_clean[-Reduce(intersect, list(idx.p, idx.cond, idx.phytar)),]

dim(dta_clean)
dim(dta_clean2)

##########################################
# check if people answer correctly...
# compare distErr and Ans, Target distance
# check position response (distErr) remove inaccurate responses
# dta_y's conditions and layouts (5,6,7) have some mistakes. Need to check that
# Set a threshold for baseline distErr ... like 0.5?
##########################################
which(dta_clean2$distErr > dta_clean2$distTargets)


# this graph is just for fun. It does not make lots of sense.
dta_clean2 |> ggplot(aes(x=RT, y=distErr, color=SelfRotation)) +
  geom_point() +
  stat_smooth(aes(color = SelfRotation), method = "lm", se = F) +
  facet_grid(TargetType ~ .) +
  theme_bw()

str(dta_clean2)

# Baseline vs. Testing
# mean calculation
dta_clean.mean <- dta_clean2 |> group_by(Participant, Baseline) |>
  summarise(RT.mean = mean(RT),
            distErr.mean = mean(distErr))

ggplot(dta_clean2, aes(x=RT, fill=Baseline)) +
  geom_histogram(aes(y=after_stat(density)), color= "black", alpha=0.2, position='identity', binwidth=.200) +
  geom_density(alpha=.2) +
  geom_vline(data=dta_clean.mean, aes(xintercept=RT.mean, color=Baseline),
            linetype="dashed", linewidth=1) +
  facet_grid(Participant~.) +
  scale_fill_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) 

ggplot(dta_clean2, aes(x=distErr, fill=Baseline)) +
  geom_histogram(aes(y=after_stat(density)), color= "black", alpha=0.2, position='identity', binwidth=.05) +
  geom_density(alpha=.2) +
  geom_vline(data=dta_clean.mean, aes(xintercept=distErr.mean, color=Baseline),
             linetype="dashed", linewidth=1) +
  facet_grid(Participant~.) +
  scale_fill_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) 

#
dta_clean.cond <- dta_clean |> group_by(Participant, Baseline, Condition) |>
  summarise(RT.mean = mean(RT),
            distErr.mean = mean(distErr))

ggplot(dta_clean2, aes(x=RT, fill=Condition)) +
  geom_histogram(aes(y=after_stat(density)), color= "black", alpha=0.2, position='identity', binwidth=.2) +
  geom_density(alpha=.2) +
  geom_vline(data=dta_clean.cond, aes(xintercept=RT.mean, color=Condition),
  linetype="dashed", linewidth=1) +
  facet_grid(Participant~Baseline) +
  scale_fill_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) 

ggplot(dta_clean, aes(x=distErr, fill=Condition)) +
  geom_histogram(aes(y=after_stat(density)), color= "black", alpha=0.2, position='identity', binwidth=.05) +
  geom_density(alpha=.2) +
  geom_vline(data=dta_clean.cond, aes(xintercept=distErr.mean, color=Condition),
             linetype="dashed", linewidth=1) +
  facet_grid(Participant~Baseline) +
  scale_fill_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) 
#


## Don't forget to summarize within-participants data using method from Morey (2008).
# https://www.rdocumentation.org/packages/Rmisc/versions/1.5.1/topics/summarySEwithin


# https://gist.github.com/hauselin/a83b6d2f05b0c90c0428017455f73744#file-summarysewithin2-r
ci.rt.by.decoy <- summarySEwithin(data = dta_clean2,
                                     measurevar = "RT",
                                     withinvars = "DecoyAmount",
                                     idvar = "Participant")
ci.dist.by.decoy <- summarySEwithin(data = dta_clean2,
                                         measurevar = "distErr",
                                         withinvars = "DecoyAmount",
                                         idvar = "Participant")

## Check if three decoys mess up people's performance badly
# three decoys vs. two decoys
dta_clean2 |> ggplot(aes(x=DecoyAmount, y=RT)) +
  stat_summary(fun = mean, geom = "point", size = 4) +
  geom_errorbar(data=ci.rt.by.decoy,
                aes(ymin = RT - ci, ymax = RT + ci), width = .2) +
  theme_bw()

# dta_clean2 |> ggplot(aes(x=DecoyAmount, y=RT)) +
#   stat_summary(fun = mean, geom = "point", size = 4) +
#   stat_summary(fun.data = mean_cl_boot, geom = "errorbar",
#                linetype = "solid", width = .2) +
#   theme_bw()


dta_clean2 |> ggplot(aes(x=DecoyAmount, y=distErr)) +
  stat_summary(fun = mean, geom = "point", size = 4) +
  geom_errorbar(data=ci.dist.by.decoy,
                aes(ymin = distErr - ci, ymax = distErr + ci), width = .2) +
  theme_bw()


## Check decoys, baseline vs. testing
str(dta_clean2)
dta.decoy.check <- subset(dta_clean2, dta_clean2$Baseline == "Testing")

dta.decoy.check |> ggplot(aes(x=DecoyAmount, y=RT)) +
  stat_summary(fun = mean, geom = "point", size = 4) +
  stat_summary(fun.data = mean_cl_boot, geom = "errorbar",
               linetype = "solid", width = .2) +
  facet_grid(Participant~.) +
  theme_bw()


dta.decoy.check |> ggplot(aes(x=DecoyAmount, y=distErr)) +
  stat_summary(fun = mean, geom = "point", size = 4) +
  stat_summary(fun.data = mean_cl_boot, geom = "errorbar",
               linetype = "solid", width = .2) +
  facet_grid(Participant~.) +
  theme_bw()

t.test(RT ~ DecoyAmount, data = dta.decoy.check)
t.test(distErr ~ DecoyAmount, data = dta.decoy.check)

# an opportunity to run a linear mixed model here
# - participant: randome effect
# - decoy: fixed effect

# https://psych252.github.io/psych252book/
library("lme4")
lmer(formula = RT ~ DecoyAmount + (1 | Participant),
     data = dta.decoy.check) |> 
  summary()


fit.compact <- lmer(formula = RT ~ 1 + (1 | Participant),
                    data = dta.decoy.check)
fit.augmented <- lmer(formula = RT ~ DecoyAmount + (1 | Participant),
                    data = dta.decoy.check)
anova(fit.compact, fit.augmented)
################################# Baseline

baseline.ci.rt <- summarySEwithin(data = dta_clean2,
                                   measurevar = "RT",
                                   withinvars = c("Baseline"),
                                   idvar = "Participant")

baseline.ci.dist <- summarySEwithin(data = dta_clean2,
                                     measurevar = "distErr",
                                     withinvars = c("Baseline"),
                                     idvar = "Participant")
#
baseline.ci.rt |>
  ggplot(aes(x=reorder(Baseline, RT, mean),
             y=RT)) +
    geom_pointrange(aes(ymin = RT - ci, ymax = RT + ci)) +
    labs(x="Response Type", y="Reaction Time (s)") +
    theme_bw()

baseline.ci.dist |>
  ggplot(aes(x=reorder(Baseline, distErr, mean),
             y=distErr)) +
  geom_pointrange(aes(ymin = distErr - ci, ymax = distErr + ci)) +
  labs(x="Response Type", y="Absolut Position Error (m)") +
  theme_bw()

#################################

base.cond.ci.rt <- summarySEwithin(data = dta_clean2,
                                   measurevar = "RT",
                                   withinvars = c("Condition", "Baseline"),
                                   idvar = "Participant")

base.cond.ci.dist <- summarySEwithin(data = dta_clean2,
                                     measurevar = "distErr",
                                     withinvars = c("Condition", "Baseline"),
                                     idvar = "Participant")


# RT summarized by conditions
base.cond.ci.rt |>
  ggplot(aes(x=reorder(Baseline, RT, mean), y=RT,
             shape=Condition, group=Condition, color=Condition)) +
  geom_pointrange(aes(ymin = RT - ci, ymax = RT + ci),
                  position=position_dodge(width=.1)) +
  geom_line(linetype="dashed", size=.5,
            position=position_dodge(width=.1)) +
  scale_shape_manual(values=c(18, 17, 20, 15)) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  labs(x="Response Type", y="Reaction Time (s)") +
  theme_bw()


# improved plot

base.cond.ci.rt2 <- summarySEwithin(data = dta_clean2,
                                   measurevar = "RT",
                                   withinvars = c("TargetType", "SelfRotation", "Baseline"),
                                   idvar = "Participant")
base.cond.ci.rt2
base.cond.ci.rt

base.target.ci.rt <- summarySEwithin(data = dta_clean2,
                                   measurevar = "RT",
                                   withinvars = c("TargetType", "Baseline"),
                                   idvar = "Participant")

base.target.ci.rt <- subset(base.target.ci.rt, Baseline == "Baseline")
base.cond.ci.rt2 <- subset(base.cond.ci.rt2, Baseline == "Testing")

base.cond.ci.rt2 <- base.cond.ci.rt2[, -2]

dta.adapt.plot <- rbind(base.cond.ci.rt2, base.target.ci.rt)

dta.adapt.plot$self.rotation <- c("none", "rotate", "none", "rotate", "none", "none")
dta.adapt.plot$for.plot <- c("none", "rotate", "none", "rotate", "rotate", "rotate")


dta.adapt.plot |>
  ggplot(aes(x=reorder(Baseline, RT, mean), y=RT,
             shape=self.rotation, color=TargetType)) +
  geom_pointrange(aes(ymin = RT - ci, ymax = RT + ci),
                  position=position_dodge(width=.1)) +
  geom_line(aes(group=interaction(TargetType, self.rotation)),
            linetype="dashed", linewidth=.5, 
            position=position_dodge(width=.1)) +
  geom_line(aes(group=interaction(TargetType, for.plot)),
            linetype="dashed", linewidth=.5, 
            position=position_dodge(width=.1)) +
  scale_shape_manual(values=c(18, 17, 20, 15)) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  labs(x="Response Type", y="Reaction Time (s)") +
  theme_bw()




# dist
base.cond.ci.dist |>
  ggplot(aes(x=reorder(Baseline, distErr, mean), y=distErr,
             shape=Condition, group=Condition, color=Condition)) +
  geom_pointrange(aes(ymin = distErr - ci, ymax = distErr + ci),
                  position=position_dodge(width=.1)) +
  geom_line(linetype="dashed", size=.5,
            position=position_dodge(width=.1)) +
  scale_shape_manual(values=c(18, 17, 20, 15)) +
  scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
  labs(x="Response Type", y="Absolut Position Error (m)") +
  theme_bw()


###
# dta_by_p <- dta_clean2 |> group_by(Participant, Baseline, Condition) |>
#   summarise(rt_m = mean(RT),
#             dist_m = mean(distErr))
# 
# levels(dta_by_p$Baseline) <- c("Testing", "Baseline")

# dta_by_p |>
#   ggplot(aes(x=reorder(dta_by_p$Baseline, dta_by_p$rt_m, mean), y=rt_m)) +
#     stat_summary(fun = mean, geom = "point", size = 4) +
#     stat_summary(fun.data = mean_cl_boot, geom = "errorbar",
#                  linetype = "solid", width = .2) +
#     labs(x="Response Type", y="Reaction Time (s)") +
#     theme_bw()
# 
# dta_by_p |>
#   ggplot(aes(x=reorder(dta_by_p$Baseline, dta_by_p$dist_m, mean), y=dist_m)) +
#   stat_summary(fun = mean, geom = "point", size = 4) +
#   stat_summary(fun.data = mean_cl_boot, geom = "errorbar",
#                linetype = "solid", width = .2) +
#   labs(x="Response Type", y="Absolut Position Error (m)") +
#   theme_bw()


# RT summarized by conditions
# dta_by_p |>
#   ggplot(aes(x=reorder(dta_by_p$Baseline, dta_by_p$rt_m, mean), y=rt_m,
#              shape=Condition, group=Condition)) +
#   stat_summary(fun = mean, geom = "point", size = 2,
#                position = position_dodge(width=.2)) +
#   stat_summary(fun.data = mean_cl_boot, geom = "pointrange", linetype = "solid",
#                position = position_dodge(width=.2)) +
#   stat_summary(fun.y = mean, geom = "line",
#                position = position_dodge(width=.2),
#                linetype="dashed", size=.5) +
#   scale_shape_manual(values=c(18, 17, 20, 15)) +
#   scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
#   labs(x="Response Type", y="Reaction Time (s)") +
#   theme_bw() 

# dist
# dta_by_p |>
#   ggplot(aes(x=reorder(dta_by_p$Baseline, dta_by_p$dist_m, mean), y=dist_m,
#              shape=Condition, group=Condition)) +
#   stat_summary(fun = mean, geom = "point", size = 2,
#                position = position_dodge(width=.2)) +
#   stat_summary(fun.data = mean_cl_boot, geom = "pointrange", linetype = "solid",
#                position = position_dodge(width=.2)) +
#   stat_summary(fun.y = mean, geom = "line",
#                position = position_dodge(width=.2),
#                linetype="dashed", size=.5) +
#   scale_shape_manual(values=c(18, 17, 20, 15)) +
#   scale_color_manual(values=c("#D81B60", "#1E88E5", "#FFC107", "#004D40")) +
#   labs(x="Response Type", y="Absolut Position Error (m)") +
#   theme_bw() 

# summary statistics (need more update from here)
# dta_by_p |> group_by(Condition, Baseline) |>
#   summarise(rt.m = mean(rt_m),
#             rt.sd = sd(rt_m),
#             dist.m = mean(dist_m),
#             dist.sd = sd(dist_m))

# delta RT vs. Presence
# dta_by_p2 <- dta_clean2 |> group_by(Participant, Baseline, Condition, TargetType, SelfRotation) |>
#   summarise(rt_m = mean(RT),
#             dist_m = mean(distErr))
# 
# levels(dta_by_p2$Baseline) <- c("Testing", "Baseline")


# physical static - virtual static
# a <- dta_by_p2 |> filter(Baseline == "Testing" & SelfRotation == "none" &
#                       TargetType == "physicalTarget") 
# b <- dta_by_p2 |> filter(Baseline == "Testing" & SelfRotation == "none" &
#                       TargetType == "virtualTarget") 
# a$deltaRT <- a$rt_m - b$rt_m
# 
# # physical rotate - virtual rotate
# a2 <- dta_by_p2 |> filter(Baseline == "Testing" & SelfRotation == "rotate" &
#                       TargetType == "physicalTarget") 
# b2 <- dta_by_p2 |> filter(Baseline == "Testing" & SelfRotation == "rotate" &
#                       TargetType == "virtualTarget") 



# a2$deltaRT <- a2$rt_m - b2$rt_m
# 
# dta_model <- rbind(a, a2)
# 
# t.test(deltaRT ~ SelfRotation, data = dta_model, paired = TRUE)
# 
# spes <- dta_q |> group_by(ParticipantID) |> summarise(m = mean(Response))
# 
# dta_model$SPES <- rep(spes$m, 2)
# 
# plot(dta_model$deltaRT[4:6], dta_model$SPES[4:6])
# plot(a2$rt_m, spes$m)

# some models (n=3)
# with presence?

