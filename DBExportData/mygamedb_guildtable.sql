-- MySQL dump 10.13  Distrib 8.0.32, for Win64 (x86_64)
--
-- Host: 192.168.123.1    Database: mygamedb
-- ------------------------------------------------------
-- Server version	5.7.41-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `guildtable`
--

DROP TABLE IF EXISTS `guildtable`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `guildtable` (
  `Guild_uid` bigint(20) NOT NULL,
  `Guild_Name` varchar(45) COLLATE utf8_unicode_ci DEFAULT NULL,
  `Guild_crews` mediumtext COLLATE utf8_unicode_ci,
  `Guild_leader` bigint(20) DEFAULT NULL,
  `Guild_JoinRequestUser` mediumtext COLLATE utf8_unicode_ci,
  PRIMARY KEY (`Guild_uid`),
  UNIQUE KEY `Guild_uid_UNIQUE` (`Guild_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `guildtable`
--

LOCK TABLES `guildtable` WRITE;
/*!40000 ALTER TABLE `guildtable` DISABLE KEYS */;
INSERT INTO `guildtable` VALUES (638514038234632057,'123','[{\"crewUid\":638506276349467625,\"crewName\":\"서혜성\"}]',638506276349467625,'[638509662749372671]'),(638515000213264767,'525','[{\"crewUid\":638509663228088539,\"crewName\":\"장두\"}]',638509663228088539,NULL);
/*!40000 ALTER TABLE `guildtable` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2024-05-17 16:16:01
