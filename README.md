# AIForAirline
天池大赛 智慧航空AI大赛 用Git

问题1：
至少停留50分钟
如果上一个航班 EndTime： 12:00
那么下一个航班 StartTime 12:50 还是 12:51
同样的问题还有旅客中转时间 进港在12：00 出港航班是13：30分，还是13：31分（假设90分钟停留）

问题2：中转时间的软约束
这个在惩罚函数中没有体现出来

问题3：
停机故障，停机数0的意思是不允许停机吧

问题4：
边界禁止条件中，最早最晚的概念是这次提供的数据中的最早最晚？还是每天的最早最晚？