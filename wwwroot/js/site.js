// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Script para forzar alineación de columnas en todas las tablas
$(document).ready(function() {
    // Agregar estilos adicionales para mejorar la alineación
    $('<style>\n\
        /* Estilos para mejorar la alineación de las tablas */\n\
        .dataTable {\n\
            border-collapse: collapse !important;\n\
            width: 100% !important;\n\
        }\n\
        .dataTable th, .dataTable td {\n\
            border: 1px solid #e3e6f0 !important;\n\
        }\n\
        .dataTable thead th {\n\
            border-bottom: 2px solid #e3e6f0 !important;\n\
        }\n\
    </style>').appendTo('head');
    
    // Este script se ejecuta después de que todas las tablas se hayan inicializado
    setTimeout(function() {
        // Forzar un ordenamiento inicial en todas las tablas DataTable
        // Esto hace que las columnas se recalculen y las líneas verticales se alineen correctamente
        try {
            $('.dataTable').each(function() {
                try {
                    var table = $(this).DataTable();
                    // Solo aplicar orden si la tabla tiene datos
                    if (table.data().length > 0) {
                        table.draw();
                    }
                } catch (e) {
                    console.log('Error al procesar tabla:', e);
                }
            });
        } catch (e) {
            console.log('Error general:', e);
        }
    }, 100);
});

// Configuración común para DataTables
var dataTableConfig = {
    // Configuración para evitar scroll
    scrollX: false,        // Sin scroll horizontal
    scrollY: false,        // Sin scroll vertical
    paging: true,          // Habilitar paginación
    pageLength: 10,        // Mayor número de registros por página para reducir necesidad de paginación
    autoWidth: true,
    responsive: true,
    // Forzar ordenamiento inicial para corregir alineación
    order: [[0, 'asc']],
    // Idioma español
    language: {
        url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json'
    },

    // Estilos personalizados
    initComplete: function() {
        // Aplicar estilos personalizados para el DataTable
        $('<style>\n\
        /* Estilos para el buscador y controles de DataTable */\n\
        .dataTables_wrapper .dataTables_length,\n\
        .dataTables_wrapper .dataTables_filter {\n\
            margin-bottom: 5px;\n\
            padding: 3px 0;\n\
        }\n\
        \n\
        /* Alinear el buscador a la derecha con el mismo padding que el selector */\n\
        .dataTables_wrapper .dataTables_filter {\n\
            text-align: right;\n\
            padding-right: 15px;\n\
        }\n\
        \n\
        /* Alinear el selector de registros a la izquierda */\n\
        .dataTables_wrapper .dataTables_length {\n\
            padding-left: 15px;\n\
        }\n\
        \n\
        /* Espacio entre los controles y la tabla */\n\
        .dataTables_wrapper .row:first-child {\n\
            margin-bottom: 5px;\n\
        }\n\
        \n\
        /* Ajustar el ancho de la tabla al contenedor */\n\
        .table {\n\
            width: 100% !important;\n\
        }\n\
        \n\
        /* Asegurar que las celdas no se desborden */\n\
        .table td, .table th {\n\
            white-space: normal;\n\
            word-break: break-word;\n\
        }\n\
        \n\
        /* Ajustes para evitar scroll vertical */\n\
        .dataTables_wrapper {\n\
            margin-bottom: 10px;\n\
        }\n\
        \n\
        /* Ajustar el espaciado de los controles de paginación */\n\
        .dataTables_paginate {\n\
            margin-top: 5px !important;\n\
            padding-top: 0.3em !important;\n\
        }\n\
        \n\
        /* Ajustar el tamaño de los controles de paginación */\n\
        .paginate_button {\n\
            padding: 0.2em 0.6em !important;\n\
            font-size: 0.875rem !important;\n\
        }\n\
        \n\
        /* Ajustar tamaño de texto en controles de DataTable */\n\
        .dataTables_info, \n\
        .dataTables_length, \n\
        .dataTables_filter {\n\
            font-size: 0.875rem !important;\n\
        }\n\
        \n\
        /* Reducir espacios en las cards */\n\
        .card-header {\n\
            padding: 0.5rem 1rem !important;\n\
        }\n\
        \n\
        .card-body {\n\
            padding: 0.75rem !important;\n\
        }\n\
        \n\
        /* Ajustar espacios en las tablas */\n\
        .table>:not(caption)>*>* {\n\
            padding: 0.4rem 0.5rem !important;\n\
            line-height: 1 !important;\n\
        }\n\
        \n\
        /* Ajustar altura de las filas para mejorar alineación */\n\
        .table tbody tr {\n\
            height: 40px !important;\n\
        }\n\
        \n\
        /* Forzar alineación de bordes */\n\
        .table th, .table td {\n\
            box-sizing: border-box !important;\n\
        }\n\
        \n\
        /* Ajustar tamaño de títulos y encabezados */\n\
        h1, .h1 { font-size: 1.8rem !important; }\n\
        h2, .h2 { font-size: 1.6rem !important; margin-bottom: 0.5rem !important; margin-top: 1rem !important; }\n\
        h3, .h3 { font-size: 1.35rem !important; margin-bottom: 0.5rem !important; }\n\
        h4, .h4 { font-size: 1.2rem !important; }\n\
        h5, .h5 { font-size: 1.1rem !important; }\n\
        h6, .h6 { font-size: 1rem !important; }\n\
        \n\
        /* Ajustar tamaño de texto en tablas */\n\
        .table {\n\
            font-size: 0.875rem !important;\n\
        }\n\
        \n\
        /* Ajustar tamaño de texto en encabezados de tabla */\n\
        .table thead th {\n\
            font-size: 0.9rem !important;\n\
            font-weight: 600 !important;\n\
        }\n\
        \n\
        /* Reducir tamaño del footer */\n\
        .footer {\n\
            padding-top: 0.3rem !important;\n\
            padding-bottom: 0.3rem !important;\n\
            font-size: 0.75rem !important;\n\
        }\n\
        \n\
        /* Reducir tamaño de los controles de formulario */\n\
        .form-control {\n\
            padding: 0.25rem 0.5rem !important;\n\
            font-size: 0.875rem !important;\n\
            min-height: calc(1.5em + 0.5rem + 2px) !important;\n\
            height: calc(1.5em + 0.5rem + 2px) !important;\n\
        }\n\
        \n\
        /* Reducir tamaño de las etiquetas */\n\
        .form-label {\n\
            margin-bottom: 0.25rem !important;\n\
            font-size: 0.875rem !important;\n\
        }\n\
        \n\
        /* Reducir tamaño de los botones */\n\
        .btn {\n\
            padding: 0.25rem 0.5rem !important;\n\
            font-size: 0.875rem !important;\n\
        }\n\
        \n\
        /* Ajustar tamaño de los botones pequeños */\n\
        .btn-sm {\n\
            padding: 0.15rem 0.4rem !important;\n\
            font-size: 0.75rem !important;\n\
        }\n\
        \n\
        /* Reducir espacios en general */\n\
        .mb-4 {\n\
            margin-bottom: 1rem !important;\n\
        }\n\
        \n\
        .mb-3 {\n\
            margin-bottom: 0.75rem !important;\n\
        }\n\
        \n\
        .mb-2 {\n\
            margin-bottom: 0.5rem !important;\n\
        }\n\
        </style>').appendTo('head');
        
        // Forzar la aplicación de estilos después de que DataTable se ha inicializado
        setTimeout(function() {
            $('.dataTables_filter, .dataTables_length').css('margin-top', '10px');
        }, 100);
    }
};
