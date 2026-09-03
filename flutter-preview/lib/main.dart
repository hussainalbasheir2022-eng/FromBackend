import 'package:flutter/material.dart';
import 'app.dart';
import 'update/update_agent.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  UpdateAgent.instance.start();
  runApp(const MyApp());
}