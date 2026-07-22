import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;

/// Excepción "bonita" para mostrar al usuario.
class BorradorDevApiException implements Exception {
  final int statusCode;
  final String message;
  final String? rawBody;

  BorradorDevApiException(this.statusCode, this.message, {this.rawBody});

  @override
  String toString() => message;
}

class BorradorDevApi {
  final String baseUrl;
  BorradorDevApi(this.baseUrl);

  static const _timeout = Duration(seconds: 12);

  // ----------------- Helpers -----------------

  String _cleanUrl(String s) => s.trim().replaceAll(RegExp(r'\/+$'), '');

  Uri _u(String path) => Uri.parse('${_cleanUrl(baseUrl)}$path');

  Map<String, dynamic> _decodeMap(String body) {
    final obj = jsonDecode(body);
    if (obj is Map<String, dynamic>) return obj;
    throw FormatException("Respuesta inválida (no es JSON objeto).");
  }

  String _extractErrorMessage(String body) {
    final b = body.trim();
    if (b.isEmpty) return "Error desconocido";

    // Intenta parsear JSON
    try {
      final obj = jsonDecode(b);

      // Caso: { ok:false, error:"...", inner:"..." }
      if (obj is Map<String, dynamic>) {
        // ASP.NET ProblemDetails típico:
        // { type, title, status, detail, traceId, errors }
        String? pickStr(dynamic v) {
          if (v is String && v.trim().isNotEmpty) return v.trim();
          return null;
        }

        final err = pickStr(obj['error']);
        final msg = pickStr(obj['message']);
        final title = pickStr(obj['title']);
        final detail = pickStr(obj['detail']);
        final inner = pickStr(obj['inner']);

        // Validation errors: { errors: { Field: ["msg1","msg2"] } }
        String? validation;
        final errors = obj['errors'];
        if (errors is Map) {
          final parts = <String>[];
          errors.forEach((k, v) {
            if (v is List) {
              for (final it in v) {
                final s = it?.toString().trim();
                if (s != null && s.isNotEmpty) {
                  parts.add("$k: $s");
                }
              }
            } else if (v != null) {
              final s = v.toString().trim();
              if (s.isNotEmpty) parts.add("$k: $s");
            }
          });
          if (parts.isNotEmpty) validation = parts.join("\n");
        }

        final pieces = <String>[];

        // Prioridad: error > message > title > detail > validation
        if (err != null) pieces.add(err);
        else if (msg != null) pieces.add(msg);
        else if (title != null) pieces.add(title);
        else if (detail != null) pieces.add(detail);

        if (validation != null) {
          // si no hay mensaje principal, igual mostramos validación
          if (pieces.isEmpty) pieces.add("Validación:");
          pieces.add(validation);
        }

        if (inner != null && inner != "null") pieces.add(inner);

        if (pieces.isNotEmpty) return pieces.join("\n");
      }
    } catch (_) {
      // no es JSON
    }

    // Si no es JSON, devolvemos el body tal cual
    return b;
  }

  Never _throwHttp(http.Response res) {
    final msg = _extractErrorMessage(res.body);
    throw BorradorDevApiException(res.statusCode, msg, rawBody: res.body);
  }

  // ----------------- Endpoints -----------------

  /// GET /borrador-dev/list?top=20&q=...&status=0
  Future<List<Map<String, dynamic>>> list({String? q, int top = 50, int? status}) async {
    final uri = _u('/borrador-dev/list').replace(queryParameters: {
      'top': top.toString(),
      if (q != null && q.trim().isNotEmpty) 'q': q.trim(),
      if (status != null) 'status': status.toString(),
    });

    final res = await http.get(uri).timeout(_timeout);
    if (res.statusCode < 200 || res.statusCode >= 300) _throwHttp(res);

    final json = _decodeMap(res.body);
    if (json['ok'] != true) {
      throw BorradorDevApiException(res.statusCode, _extractErrorMessage(res.body), rawBody: res.body);
    }

    final items = (json['items'] as List?) ?? const [];
    return items.cast<Map>().map((e) => e.cast<String, dynamic>()).toList();
  }

  /// POST /borrador-dev/create
  /// Nuevo contrato: Item y Costo NO se envían.
  /// Retorna: { ok:true, numero: ..., inserted, updated, detalles }
  Future<int> create({
    required String usuario,
    required String observacion,
    String? fechaYmd, // "yyyy-MM-dd" o null
    required String codigoBarra,
    int numeroCaja = 1,
    num cantidad = 1,
    int status = 0,
  }) async {
    final uri = _u('/borrador-dev/create');

    final body = {
      "fecha": fechaYmd,
      "observacion": observacion,
      "usuario": usuario,
      "status": status,
      "detalles": [
        {
          "codigoBarra": codigoBarra,
          "cantidad": cantidad,
          "numeroCaja": numeroCaja,
        }
      ]
    };

    final res = await http
        .post(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(body))
        .timeout(_timeout);

    if (res.statusCode < 200 || res.statusCode >= 300) _throwHttp(res);

    final json = _decodeMap(res.body);
    if (json['ok'] != true) {
      throw BorradorDevApiException(res.statusCode, _extractErrorMessage(res.body), rawBody: res.body);
    }

    final numero = json['numero'];
    if (numero is int) return numero;
    if (numero is num) return numero.toInt();
    throw BorradorDevApiException(res.statusCode, 'Respuesta sin "numero".', rawBody: res.body);
  }

  /// POST /borrador-dev/{numero}/scan
  /// Lógica backend: si existe (Numero+CodigoBarra+NumeroCaja) => Cantidad += cantidad
  /// si no existe => inserta nueva línea con Item autogenerado
  Future<Map<String, dynamic>> scan({
    required int numero,
    required String codigoBarra,
    int numeroCaja = 1,
    num cantidad = 1,
  }) async {
    final uri = _u('/borrador-dev/$numero/scan');

    final body = {
      "detalles": [
        {
          "codigoBarra": codigoBarra,
          "cantidad": cantidad,
          "numeroCaja": numeroCaja,
        }
      ]
    };

    final res = await http
        .post(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(body))
        .timeout(_timeout);

    if (res.statusCode < 200 || res.statusCode >= 300) _throwHttp(res);

    final json = _decodeMap(res.body);
    if (json['ok'] != true) {
      throw BorradorDevApiException(res.statusCode, _extractErrorMessage(res.body), rawBody: res.body);
    }
    return json;
  }

  /// POST /borrador-dev/run
  Future<Map<String, dynamic>> run({required int numero, required bool aprobar}) async {
    final uri = _u('/borrador-dev/run');
    final body = {"numero": numero, "aprobar": aprobar};

    final res = await http
        .post(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(body))
        .timeout(_timeout);

    if (res.statusCode < 200 || res.statusCode >= 300) _throwHttp(res);

    final json = _decodeMap(res.body);
    if (json['ok'] != true) {
      throw BorradorDevApiException(res.statusCode, _extractErrorMessage(res.body), rawBody: res.body);
    }
    return json;
  }

  /// GET /borrador-dev/{numero}
  Future<Map<String, dynamic>?> getDetalle(int numero) async {
    final uri = _u('/borrador-dev/$numero');
    final res = await http.get(uri).timeout(_timeout);

    if (res.statusCode == 404) return null;
    if (res.statusCode < 200 || res.statusCode >= 300) _throwHttp(res);

    final json = _decodeMap(res.body);
    if (json['ok'] != true) {
      throw BorradorDevApiException(res.statusCode, _extractErrorMessage(res.body), rawBody: res.body);
    }

    if (json['header'] == null) return null;
    return json;
  }

  /// PUT /borrador-dev/{numero}/detalle
  /// (se usa en la vista DetalleScreen: set manual)
  Future<Map<String, dynamic>> updateDetalle({
    required int numero,
    required List<Map<String, dynamic>> detalles,
  }) async {
    final uri = _u('/borrador-dev/$numero/detalle');
    final body = {"detalles": detalles};

    final res = await http
        .put(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(body))
        .timeout(_timeout);

    if (res.statusCode < 200 || res.statusCode >= 300) _throwHttp(res);

    final json = _decodeMap(res.body);
    if (json['ok'] != true) {
      throw BorradorDevApiException(res.statusCode, _extractErrorMessage(res.body), rawBody: res.body);
    }
    return json;
  }
}
